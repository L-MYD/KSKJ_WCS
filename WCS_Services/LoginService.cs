using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WCS_Common.Enums;
using WCS_Common.Helper;
using WCS_Helper;
using WCS_Helper.Database;
using WCS_IServices;
using WCS_Models.LoginViewModel;

namespace WCS_Services
{
    /// <summary>
    /// nan_T 2026-09-09：登录/用户管理服务实现（参考 DTcms.Core.Services.Base.UserService 的分层写法）。
    /// 继承 <see cref="BaseService"/> 获得泛型 CRUD 能力，通过 <see cref="IDbContextFactory"/>
    /// 创建数据库上下文，不再直接依赖静态的 UserDbContext 仓储类；
    /// 业务规则不满足时抛出 <see cref="ResponseException"/>，由全局异常过滤器统一转换响应。
    /// </summary>
    public class LoginService : BaseService, ILoginService
    {
        /// <summary>nan_T 2026-09-09：日志记录器（可选注入，未注入时静默跳过日志）</summary>
        private readonly ILogger<LoginService>? _logger;

        /// <summary>
        /// 构造函数：注入数据库工厂与日志组件
        /// </summary>
        /// <param name="contextFactory">数据库上下文工厂（由 DI 容器注入）</param>
        /// <param name="logger">日志记录器（可选）</param>
        public LoginService(IDbContextFactory contextFactory, ILogger<LoginService>? logger = null)
            : base(contextFactory)
        {
            _logger = logger;
        }

        /// <summary>
        /// 校验用户名与密码，验证通过后返回用户实体。
        /// 流程：查用户 → PBKDF2 哈希校验密码（兼容旧明文并自动升级）→ 校验启用状态 → 更新最后登录时间。
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">明文密码</param>
        /// <returns>校验通过的用户实体</returns>
        /// <exception cref="ResponseException">用户不存在/密码错误/账号禁用时抛出</exception>
        public async Task<LoginSqlModel> ValidateUserCredentials(string username, string password)
        {
            // 1. 参数非空校验
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new ResponseException("用户名和密码不能为空", ErrorCode.ParameterError, 422);
            }

            // 2. 查询用户（只读库）
            using var readContext = _contextFactory.CreateContext(WriteRoRead.Read);
            var user = await readContext.Users.FirstOrDefaultAsync(u => u.UserName == username);

            // nan_T 2026-09-09：密码为 PBKDF2 哈希校验；needsUpgrade=true 表示库中仍是历史明文密码
            var verifyResult = user != null
                ? PasswordHasher.Verify(password, user.Password)
                : (isValid: false, needsUpgrade: false);

            // 3. 用户不存在或密码错误
            if (user == null || !verifyResult.isValid)
            {
                _logger?.LogWarning("用户 {Username} 登录失败：用户不存在或密码错误", username);
                throw new ResponseException("用户名或密码错误", ErrorCode.Unauthorized, 401);
            }

            // 4. 启用状态校验（"1"/"true" 视为启用，兼容两种历史写法）
            if (!IsUserActive(user.IsActive))
            {
                _logger?.LogWarning("用户 {Username} 已被禁用", username);
                throw new ResponseException("账号已被禁用，请联系管理员", ErrorCode.Forbidden, 403);
            }

            // 5. 登录成功后的写操作：更新最后登录时间；旧明文密码顺带升级为哈希
            using var writeContext = _contextFactory.CreateContext(WriteRoRead.Write);
            var tracked = await writeContext.Users.FirstAsync(u => u.Id == user.Id);
            tracked.LastLoginTime = DateTimeHelper.CnNow;

            if (verifyResult.needsUpgrade)
            {
                // nan_T 2026-09-09：明文密码升级失败不影响本次登录，下次登录会重试
                try
                {
                    tracked.Password = PasswordHasher.Hash(password);
                    _logger?.LogInformation("用户 {Username} 的明文密码已自动升级为哈希存储", username);
                }
                catch (Exception upgradeEx)
                {
                    _logger?.LogWarning(upgradeEx, "用户 {Username} 密码哈希升级失败，下次登录将重试", username);
                }
            }

            await writeContext.SaveChangesAsync();

            _logger?.LogInformation("用户 {Username} 登录成功", username);
            return user;
        }

        /// <summary>
        /// 新增用户（注册）。密码入库前做 PBKDF2 哈希，并设置启用状态/角色默认值。
        /// </summary>
        /// <param name="user">待新增用户信息（密码为明文）</param>
        /// <returns>新用户的主键 Id</returns>
        /// <exception cref="ResponseException">参数为空或用户名已存在时抛出</exception>
        public async Task<int> InsertUserCredentials(LoginSqlModel user)
        {
            // 1. 参数校验
            if (user == null)
            {
                throw new ResponseException("请求参数不能为空", ErrorCode.ParameterError, 422);
            }
            if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                throw new ResponseException("用户名和密码不能为空", ErrorCode.ParameterError, 422);
            }

            // 2. 用户名唯一性检查
            using (var readContext = _contextFactory.CreateContext(WriteRoRead.Read))
            {
                bool exists = await readContext.Users.AnyAsync(u => u.UserName == user.UserName);
                if (exists)
                {
                    _logger?.LogWarning("插入用户失败：用户名 {UserName} 已存在", user.UserName);
                    throw new ResponseException($"用户名 {user.UserName} 已存在", ErrorCode.Fail, 400);
                }
            }

            // 3. 默认值：默认启用、默认角色为观察员(2)
            user.IsActive = string.IsNullOrEmpty(user.IsActive) ? "1" : user.IsActive;
            user.UserRole = string.IsNullOrEmpty(user.UserRole) ? "2" : user.UserRole;
            user.RoleId = MapRoleId(user.UserRole);
            user.LastLoginTime = DateTimeHelper.CnNow;

            // nan_T 2026-09-09：明文密码先哈希再落库，数据库不保存明文
            user.Password = PasswordHasher.Hash(user.Password);

            // 4. 写入数据库
            using var writeContext = _contextFactory.CreateContext(WriteRoRead.Write);
            await writeContext.Users.AddAsync(user);
            await writeContext.SaveChangesAsync(); //保存后 user.Id 由数据库自增回填

            _logger?.LogInformation("用户 {UserName} 插入成功，ID: {Id}", user.UserName, user.Id);
            return user.Id;
        }

        /// <summary>
        /// 更新用户信息（部分更新：请求中未携带的字段保留库中原值，不会被清空）。
        /// </summary>
        /// <param name="user">待更新的用户信息（Id 必填）</param>
        /// <returns>更新后的用户实体</returns>
        /// <exception cref="ResponseException">用户不存在时抛出</exception>
        public async Task<LoginSqlModel> UpdateUserAsync(LoginSqlModel user)
        {
            if (user == null || user.Id <= 0)
            {
                throw new ResponseException("用户信息无效", ErrorCode.ParameterError, 422);
            }

            using var context = _contextFactory.CreateContext(WriteRoRead.Write);
            var existing = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existing == null)
            {
                throw new ResponseException($"用户ID {user.Id} 不存在", ErrorCode.NotFound, 404);
            }

            // nan_T 2026-09-09：空值字段保留原值，防止未传字段把用户名/密码清空
            if (!string.IsNullOrEmpty(user.UserName))
                existing.UserName = user.UserName;

            if (!string.IsNullOrEmpty(user.Password))
            {
                // nan_T 2026-09-09：已是哈希格式则直接保存（防重复哈希），明文则先哈希
                existing.Password = user.Password.StartsWith("PBKDF2:", StringComparison.Ordinal)
                    ? user.Password
                    : PasswordHasher.Hash(user.Password);
            }

            if (!string.IsNullOrEmpty(user.UserRole))
            {
                existing.UserRole = user.UserRole;
                existing.RoleId = MapRoleId(user.UserRole);
            }

            if (!string.IsNullOrEmpty(user.IsActive))
                existing.IsActive = user.IsActive;

            if (user.LastLoginTime != default)
                existing.LastLoginTime = user.LastLoginTime;

            await context.SaveChangesAsync();

            _logger?.LogInformation("用户ID {Id} 更新成功", user.Id);
            return existing;
        }

        /// <summary>
        /// 获取全部用户列表（按 Id 升序）
        /// </summary>
        /// <returns>用户实体列表</returns>
        public async Task<List<LoginSqlModel>> GetUserList()
        {
            using var context = _contextFactory.CreateContext(WriteRoRead.Read);
            var userList = await context.Users.OrderBy(u => u.Id).ToListAsync();
            _logger?.LogInformation("获取用户列表成功，共 {Count} 个用户", userList.Count);
            return userList;
        }

        /// <summary>
        /// 根据用户 Id 删除用户
        /// </summary>
        /// <param name="userId">用户 Id</param>
        /// <returns>受影响行数</returns>
        /// <exception cref="ResponseException">用户不存在时抛出</exception>
        public async Task<int> DeleteUser(int userId)
        {
            if (userId <= 0)
            {
                throw new ResponseException("用户ID无效", ErrorCode.ParameterError, 422);
            }

            using var context = _contextFactory.CreateContext(WriteRoRead.Write);
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new ResponseException($"用户ID {userId} 不存在", ErrorCode.NotFound, 404);
            }

            context.Users.Remove(user);
            int result = await context.SaveChangesAsync();

            _logger?.LogInformation("用户ID {UserId} 删除成功", userId);
            return result;
        }

        /// <summary>
        /// 根据用户 Id 查询用户信息
        /// </summary>
        /// <param name="userId">用户 Id</param>
        /// <returns>用户实体；不存在返回 null</returns>
        public async Task<LoginSqlModel?> GetUserById(int userId)
        {
            if (userId <= 0)
            {
                return null;
            }
            using var context = _contextFactory.CreateContext(WriteRoRead.Read);
            return await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// 根据用户名查询用户信息
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户实体；不存在返回 null</returns>
        public async Task<LoginSqlModel?> GetUserByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            using var context = _contextFactory.CreateContext(WriteRoRead.Read);
            return await context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        /// <summary>
        /// 修改密码；可选校验旧密码。新密码统一做 PBKDF2 哈希后落库。
        /// </summary>
        /// <param name="userId">用户 Id</param>
        /// <param name="newPassword">明文新密码</param>
        /// <param name="oldPassword">明文旧密码（不传则不校验旧密码）</param>
        /// <returns>是否修改成功</returns>
        /// <exception cref="ResponseException">用户不存在/旧密码错误时抛出</exception>
        public async Task<bool> UpdatePassword(int userId, string newPassword, string? oldPassword = null)
        {
            if (userId <= 0 || string.IsNullOrEmpty(newPassword))
            {
                throw new ResponseException("用户ID或新密码不能为空", ErrorCode.ParameterError, 422);
            }

            using var context = _contextFactory.CreateContext(WriteRoRead.Write);
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new ResponseException($"用户ID {userId} 不存在", ErrorCode.NotFound, 404);
            }

            // nan_T 2026-09-09：旧密码校验同样走哈希校验（兼容旧明文）
            if (!string.IsNullOrEmpty(oldPassword))
            {
                var oldVerify = PasswordHasher.Verify(oldPassword, user.Password);
                if (!oldVerify.isValid)
                {
                    throw new ResponseException("旧密码不正确", ErrorCode.Fail, 400);
                }
            }

            // nan_T 2026-09-09：新密码哈希后落库
            user.Password = PasswordHasher.Hash(newPassword);
            await context.SaveChangesAsync();

            _logger?.LogInformation("用户ID {UserId} 密码修改成功", userId);
            return true;
        }

        /// <summary>
        /// nan_T 2026-09-09：统一判断用户是否启用。
        /// 数据库历史数据存在 "1"、"True" 两种写法，统一视为启用；
        /// "0"、"False"、空值等一律视为禁用，避免各处判断口径不一致。
        /// </summary>
        private static bool IsUserActive(string? isActive)
        {
            if (string.IsNullOrWhiteSpace(isActive))
                return false;

            return isActive == "1"
                || string.Equals(isActive, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// nan_T 2026-09-09：角色编码到 RoleId 的映射（0=管理员，1=操作员，2=观察员）。
        /// 兼容中文历史写法；无法识别的角色默认给最低权限（观察员），不再默认管理员。
        /// </summary>
        private static int MapRoleId(string? userRole)
        {
            return userRole switch
            {
                "0" or "管理员" => 0,
                "1" or "操作员" => 1,
                "2" or "观察员" => 2,
                _ => 2
            };
        }
    }
}
