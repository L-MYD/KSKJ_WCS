using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WCS_Common.Enums;
using WCS_Common.Helper;
using WCS_IServices;
using WCS_Models.LoginViewModel;

namespace WCS_Api.Controllers
{
    /// <summary>
    /// nan_T 2026-09-09：登录与用户管理控制器（参考 DTcms.Core 控制器的瘦身写法）。
    /// 控制器只负责：参数校验 → 调用服务 → 包装统一响应 <see cref="ResponseMessage"/>；
    /// 业务错误由 Service 抛出 <see cref="ResponseException"/>，经 GlobalExceptionFilter 统一转换，
    /// 因此各 Action 不再需要重复编写 try/catch。
    /// </summary>
    //[ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        /// <summary>nan_T 2026-09-09：登录/用户服务（面向接口调用）</summary>
        private readonly ILoginService _loginService;

        /// <summary>nan_T 2026-09-09：日志记录器</summary>
        private readonly ILogger<LoginController> _logger;

        /// <summary>
        /// 构造函数：依赖注入服务与日志组件
        /// </summary>
        public LoginController(ILoginService loginService, ILogger<LoginController> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        /// <summary>
        /// 用户注册
        /// 示例：POST /api/Login/register
        /// </summary>
        /// <param name="login">注册信息（用户名、明文密码、角色等）</param>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] LoginSqlModel login)
        {
            // nan_T 2026-09-09：基础参数校验（用户名重复、密码哈希等业务规则由 Service 处理）
            if (login == null || string.IsNullOrEmpty(login.UserName) || string.IsNullOrEmpty(login.Password))
            {
                throw new ResponseException("用户名和密码不能为空", ErrorCode.ParameterError, 422);
            }

            int newId = await _loginService.InsertUserCredentials(login);
            return Ok(ResponseMessage.Success(new
            {
                id = newId,
                userName = login.UserName,
                role = login.UserRole
            }));
        }

        /// <summary>
        /// 用户登录（Cookie 认证）
        /// 示例：POST /api/Login/login
        /// </summary>
        /// <param name="login">登录凭证（用户名、密码、记住我）</param>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            // 1. 参数校验
            if (login == null || string.IsNullOrEmpty(login.username) || string.IsNullOrEmpty(login.password))
            {
                throw new ResponseException("用户名和密码不能为空", ErrorCode.ParameterError, 422);
            }

            // 2. 校验凭证（失败由 Service 抛 ResponseException，过滤器返回 401/403）
            var user = await _loginService.ValidateUserCredentials(login.username, login.password);

            // 3. 构造用户身份声明（Claims）
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.UserRole ?? "User"),
                new("LoginTime", DateTime.UtcNow.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 4. 配置认证 Cookie 属性（30 分钟过期，勾选“记住我”时持久化）
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = login.rememberme,
                IssuedUtc = DateTimeOffset.Now,
                ExpiresUtc = DateTimeOffset.Now.AddMinutes(30)
            };

            // 5. 签发认证 Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("用户 {Username} 登录成功", login.username);

            // 6. 返回登录成功响应
            return Ok(ResponseMessage.Success(new
            {
                user = new
                {
                    id = user.Id,
                    userName = user.UserName,
                    role = user.RoleId
                },
                expiresIn = 1800 //过期时间（秒），与 Cookie 过期时间保持一致
            }));
        }

        /// <summary>
        /// 注销登录（清除认证 Cookie）
        /// 示例：POST /api/Login/logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(ResponseMessage.Success("注销成功"));
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// 示例：GET /api/Login/current
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userClaims = HttpContext.User.Claims;
            return Ok(ResponseMessage.Success(new
            {
                userName = User.Identity?.Name,
                isAuthenticated = User.Identity?.IsAuthenticated,
                userId = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                role = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                loginTime = userClaims.FirstOrDefault(c => c.Type == "LoginTime")?.Value
            }));
        }

        /// <summary>
        /// 检查当前请求的认证状态（供前端路由守卫调用，无需登录即可访问）
        /// 示例：GET /api/Login/check
        /// </summary>
        [HttpGet("check")]
        [AllowAnonymous]
        public IActionResult CheckAuthStatus()
        {
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            return Ok(ResponseMessage.Success(new
            {
                isAuthenticated,
                userName = isAuthenticated ? User.Identity?.Name : null
            }));
        }

        /// <summary>
        /// 更新用户信息（管理员可改任意用户，普通用户只能改自己）
        /// 示例：PUT /api/Login/update
        /// </summary>
        /// <param name="updateModel">待更新的用户信息</param>
        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] LoginSqlModel updateModel)
        {
            if (updateModel == null)
            {
                throw new ResponseException("请求参数不能为空", ErrorCode.ParameterError, 422);
            }

            // 1. 从认证信息中解析当前用户 Id
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new ResponseException("无法识别用户身份", ErrorCode.Unauthorized, 401);
            }

            // 2. 权限校验：非管理员只能修改本人信息
            bool isAdmin = User.IsInRole("0");
            if (updateModel.Id != currentUserId && !isAdmin)
            {
                throw new ResponseException("没有权限修改其他用户信息", ErrorCode.Forbidden, 403);
            }

            // 3. 角色字段有效性校验（0-管理员，1-操作员，2-观察员）
            if (!string.IsNullOrEmpty(updateModel.UserRole) &&
                !new[] { "0", "1", "2" }.Contains(updateModel.UserRole))
            {
                throw new ResponseException("用户角色无效，只能是0-管理员，1-操作员，2-观察员", ErrorCode.ParameterError, 422);
            }

            // 4. 启用字段有效性校验（0-禁用，1-启用）
            if (!string.IsNullOrEmpty(updateModel.IsActive) &&
                !new[] { "0", "1" }.Contains(updateModel.IsActive))
            {
                throw new ResponseException("是否启用字段无效，只能是0表示禁用，1表示启用", ErrorCode.ParameterError, 422);
            }

            // 5. 执行更新（用户不存在等错误由 Service 抛异常）
            var updateResult = await _loginService.UpdateUserAsync(updateModel);
            return Ok(ResponseMessage.Success(new
            {
                id = updateResult.Id,
                username = updateResult.UserName,
                userrole = updateResult.UserRole,
                isactive = updateResult.IsActive,
                lastlogintime = updateResult.LastLoginTime
            }));
        }

        /// <summary>
        /// 获取用户列表（仅管理员）
        /// 示例：GET /api/Login/list
        /// </summary>
        [HttpGet("list")]
        [Authorize(Roles = "0")]
        public async Task<IActionResult> GetUserList()
        {
            var userList = await _loginService.GetUserList();
            return Ok(ResponseMessage.Success(userList));
        }

        /// <summary>
        /// 删除用户（仅管理员，且不能删除当前登录的自己）
        /// 示例：DELETE /api/Login/delete/1
        /// </summary>
        /// <param name="id">待删除的用户 Id</param>
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "0")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 不允许删除当前登录用户本人
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int currentUserId) && id == currentUserId)
            {
                throw new ResponseException("不能删除当前登录的用户", ErrorCode.Fail, 400);
            }

            int affectedRows = await _loginService.DeleteUser(id);
            return Ok(ResponseMessage.Success(new { affectedRows }));
        }

        /// <summary>
        /// 未登录/权限不足时 Cookie 认证跳转的统一入口，直接返回 403 JSON。
        /// 示例：GET /api/Login/access-denied
        /// </summary>
        [HttpGet("access-denied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            throw new ResponseException("访问被拒绝，权限不足", ErrorCode.Forbidden, 403);
        }
    }
}
