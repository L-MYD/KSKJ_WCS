using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using WCS_Models;
using WCS_Services;
using WCS_IServices;
using WCS_Models.SqlModel;
using WCS_Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WCS_Models.LoginViewModel;

namespace WCS_Api.Controllers
{
    //[ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILoginService loginService, ILogger<LoginController> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] LoginSqlModel login)
        {
            try
            {
                // 1. 验证输入参数
                if (login == null)
                {
                    return BadRequest(new { code = 400, message = "请求参数不能为空" });
                }

                if (string.IsNullOrEmpty(login.UserName) || string.IsNullOrEmpty(login.Password))
                {
                    return BadRequest(new { code = 400, message = "用户名和密码不能为空" });
                }

                var result = await _loginService.InsertUserCredentials(login);
                if (result > 0)
                {
                    return Ok(new
                    {
                        code = 200,
                        message = "注册成功",
                        data = new
                        {
                            id = result,
                            userName = login.UserName,
                            role = login.UserRole
                        }
                    });
                }
                else
                {
                    return BadRequest(new { code = 400, message = "注册失败，用户名可能已存在" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户注册失败");
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            try
            {
                // 1. 验证输入参数
                if (login == null)
                {
                    return BadRequest(new { code = 400, message = "请求参数不能为空" });
                }

                if (string.IsNullOrEmpty(login.username) || string.IsNullOrEmpty(login.password))
                {
                    return BadRequest(new { code = 400, message = "用户名和密码不能为空" });
                }

                // 2. 验证用户凭证
                var user = await _loginService.ValidateUserCredentials(login.username, login.password);
                if (user == null)
                {
                    return Unauthorized(new { code = 401, message = "用户名或密码错误" });
                }

                // 3. 创建用户身份声明
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.UserRole ?? "User"),
                    new Claim("LoginTime", DateTime.UtcNow.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 4. 设置认证属性
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = login.rememberme,
                    IssuedUtc = DateTimeOffset.Now,
                    ExpiresUtc = DateTimeOffset.Now.AddMinutes(30) // 30分钟过期
                };

                // 5. 登录用户，创建认证Cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // 6. 更新用户最后登录时间
                user.LastLoginTime = DateTime.UtcNow;
                await _loginService.UpdateUserAsync(user);

                // 7. 返回登录成功响应
                return Ok(new
                {
                    code = 200,
                    message = "登录成功",
                    data = new
                    {
                        user = new
                        {
                            id = user.Id,
                            userName = user.UserName,
                            role = user.RoleId
                        },
                        expiresIn = 1800 // 过期时间（秒数）
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录失败 - 用户名: {Username}", login?.username);
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 注销用户，清除Cookie
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Ok(new
                {
                    code = 200,
                    message = "注销成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户注销失败");
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpGet("current")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userClaims = HttpContext.User.Claims;

                return Ok(new
                {
                    code = 200,
                    message = "获取成功",
                    data = new
                    {
                        userName = User.Identity?.Name,
                        isAuthenticated = User.Identity?.IsAuthenticated,
                        userId = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                        role = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                        loginTime = userClaims.FirstOrDefault(c => c.Type == "LoginTime")?.Value
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户信息失败");
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpGet("check")]
        [AllowAnonymous]
        public IActionResult CheckAuthStatus()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            return Ok(new
            {
                code = 200,
                message = "检查成功",
                data = new
                {
                    isAuthenticated,
                    userName = isAuthenticated ? User.Identity?.Name : null
                }
            });
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] LoginSqlModel updateModel)
        {
            try
            {
                // 1. 验证输入参数
                if (updateModel == null)
                {
                    return BadRequest(new { code = 400, message = "请求参数不能为空" });
                }

                // 2. 从认证信息中获取当前用户ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Unauthorized(new { code = 401, message = "无法识别用户身份" });
                }

                // 3. 验证是否有权限修改
                var isAdmin = User.IsInRole("0");
                if (updateModel.Id != currentUserId && !isAdmin)
                {
                    return StatusCode(403, new { code = 403, message = "没有权限修改其他用户信息" });
                }

                // 4. 验证用户角色字段的有效性
                if (!string.IsNullOrEmpty(updateModel.UserRole) &&
                    !new[] { "0", "1", "2" }.Contains(updateModel.UserRole))
                {
                    return BadRequest(new { code = 400, message = "用户角色无效，只能是0-管理员，1-操作员，2-观察员" });
                }

                // 5. 验证是否启用字段的有效性
                if (!string.IsNullOrEmpty(updateModel.IsActive) &&
                    !new[] { "0", "1" }.Contains(updateModel.IsActive))
                {
                    return BadRequest(new { code = 400, message = "是否启用字段无效，只能是0表示禁用，1表示启用" });
                }

                // 6. 更新用户信息
                var updateResult = await _loginService.UpdateUserAsync(updateModel);
                if (updateResult == null)
                {
                    return BadRequest(new { code = 400, message = "更新失败" });
                }

                // 7. 返回成功响应
                return Ok(new
                {
                    code = 200,
                    message = "用户信息更新成功",
                    data = new
                    {
                        user = new
                        {
                            id = updateResult.Id,
                            username = updateResult.UserName,
                            userrole = updateResult.UserRole,
                            isactive = updateResult.IsActive,
                            lastlogintime = updateResult.LastLoginTime
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息失败 - 用户ID: {UserId}", updateModel?.Id);
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpGet("list")]
        [Authorize(Roles = "0")] // 只有管理员可以查看用户列表
        public async Task<IActionResult> GetUserList()
        {
            try
            {
                var userList = await _loginService.GetUserList();
                return Ok(new
                {
                    code = 200,
                    message = "获取成功",
                    data = userList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "0")] // 只有管理员可以删除用户
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                // 不能删除自己
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int currentUserId) && id == currentUserId)
                {
                    return BadRequest(new { code = 400, message = "不能删除当前登录的用户" });
                }

                var result = await _loginService.DeleteUser(id);
                if (result <= 0)
                {
                    return BadRequest(new { code = 400, message = "删除失败，用户可能不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "用户删除成功",
                    data = new { affectedRows = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败 - 用户ID: {UserId}", id);
                return StatusCode(500, new { code = 500, message = "服务器内部错误" });
            }
        }

        [HttpGet("access-denied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return StatusCode(403, new { code = 403, message = "访问被拒绝，权限不足" });
        }
    }
}