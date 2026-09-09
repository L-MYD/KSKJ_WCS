using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WCS_Models.LoginViewModel;
using WCS_Helper; // nan_T 2026-09-09：引入 PasswordHasher 密码哈希工具

namespace WCS_Helper.Mapper
{
    public static class UserDbContext
    {
        private static readonly string ConnectionString = ConfigurationHelper.GetConnectionString();

        private static WcsDbContext CreateDbContext()
        {
            return new WcsDbContext();
        }

        #region 查询

        /// <summary>
        /// 根据用户名查询用户
        /// </summary>
        public static async Task<LoginSqlModel> FindByUserNameAsync(string userName)
        {
            var stopwatch = Stopwatch.StartNew();
            string operation = "根据用户名查询用户";

            try
            {
                Debug.WriteLine($"{operation} 开始执行");

                using var context = CreateDbContext();
                var result = await context.Users
                    .FirstOrDefaultAsync(u => u.UserName == userName);

                stopwatch.Stop();
                if (result != null)
                    Debug.WriteLine($"{operation} 成功，耗时: {stopwatch.ElapsedMilliseconds}ms，查询到1条记录");
                else
                    Debug.WriteLine($"{operation} 成功，耗时: {stopwatch.ElapsedMilliseconds}ms，未查询到记录");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据用户ID查询用户
        /// </summary>
        public static async Task<LoginSqlModel> FindByIdAsync(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            string operation = "根据用户ID查询用户";

            try
            {
                Debug.WriteLine($"{operation} 开始执行");

                using var context = CreateDbContext();
                var result = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                stopwatch.Stop();
                Debug.WriteLine($"{operation} 完成，耗时: {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有用户列表
        /// </summary>
        public static async Task<List<LoginSqlModel>> GetAllUsersAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            string operation = "获取所有用户列表";

            try
            {
                Debug.WriteLine($"{operation} 开始执行");

                using var context = CreateDbContext();
                var result = await context.Users
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                stopwatch.Stop();
                Debug.WriteLine($"{operation} 完成，获取到 {result.Count} 条记录，耗时: {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 异常: {ex.Message}");
                return new List<LoginSqlModel>();
            }
        }

        /// <summary>
        /// 根据角色查询用户
        /// </summary>
        public static async Task<List<LoginSqlModel>> GetUsersByRoleAsync(string userRole)
        {
            try
            {
                using var context = CreateDbContext();
                return await context.Users
                    .Where(u => u.UserRole == userRole)
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"根据角色查询用户失败: {ex.Message}");
                return new List<LoginSqlModel>();
            }
        }

        /// <summary>
        /// 分页查询用户列表
        /// </summary>
        public static async Task<(List<LoginSqlModel> Data, int TotalCount)> GetPagedUsersAsync(int pageIndex, int pageSize, string keyword = null)
        {
            try
            {
                using var context = CreateDbContext();
                var query = context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u => u.UserName.Contains(keyword) || u.UserRole.Contains(keyword));
                }

                int totalCount = await query.CountAsync();
                var data = await query
                    .OrderBy(u => u.Id)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (data, totalCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"分页查询用户失败: {ex.Message}");
                return (new List<LoginSqlModel>(), 0);
            }
        }

        #endregion

        #region 插入

        /// <summary>
        /// 新增用户
        /// </summary>
        public static async Task<int> InsertUserAsync(LoginSqlModel user)
        {
            user.LastLoginTime = DateTime.UtcNow;

            // 根据 UserRole 设置 RoleId（如果业务需要）
            // nan_T 2026-09-09：修复角色映射不一致。接口层校验/写入的 UserRole 是 "0"/"1"/"2"，
            // 原 switch 却按中文 "操作员/管理员/观察员" 匹配，且 default 把 RoleId 置为 0（管理员），
            // 导致任意新用户都可能拿到管理员 RoleId。现统一按 "0"/"1"/"2" 映射，中文写法兼容保留。
            switch (user.UserRole)
            {
                case "0":
                case "管理员":
                    user.RoleId = 0;
                    break;
                case "1":
                case "操作员":
                    user.RoleId = 1;
                    break;
                case "2":
                case "观察员":
                    user.RoleId = 2;
                    break;
                default:
                    user.RoleId = 2; // nan_T 2026-09-09：无法识别的角色默认给最低权限（观察员），不再默认管理员
                    break;
            }

            var stopwatch = Stopwatch.StartNew();
            string operation = "新增用户";

            try
            {
                Debug.WriteLine($"{operation} 开始执行");

                using var context = CreateDbContext();
                context.Users.Add(user);
                int affected = await context.SaveChangesAsync(); // 保存后 user.Id 会自动填充

                stopwatch.Stop();
                Debug.WriteLine($"{operation} 成功，用户名: {user.UserName}, 新ID: {user.Id}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

                return user.Id; // 返回新生成的ID
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 唯一约束冲突: {ex.Message}");
                throw new Exception($"用户名 '{user.UserName}' 已存在", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 异常: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region 更新

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public static async Task<int> UpdateUserAsync(LoginSqlModel user)
        {
            var stopwatch = Stopwatch.StartNew();
            string operation = "更新用户信息";

            try
            {
                Debug.WriteLine($"{operation} 开始执行");

                using var context = CreateDbContext();
                var existing = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
                if (existing == null)
                {
                    Debug.WriteLine($"{operation} 用户不存在，ID: {user.Id}");
                    return 0;
                }

                // nan_T 2026-09-09：修复更新时空值覆盖问题。
                // 原代码无条件赋值：请求体未携带的字段为 null/默认值，会把库中已有用户名、密码清空；
                // 且 RoleId 未传时默认为 0（管理员），存在普通用户被提权的风险。
                // 现改为：空值字段保留库中原值；密码若为明文则先哈希（已是哈希格式则直接用）；
                // RoleId 依据 UserRole 重新推导，与新增用户口径一致。
                if (!string.IsNullOrEmpty(user.UserName))
                    existing.UserName = user.UserName;

                if (!string.IsNullOrEmpty(user.Password))
                {
                    existing.Password = user.Password.StartsWith("PBKDF2:", StringComparison.Ordinal)
                        ? user.Password
                        : PasswordHasher.Hash(user.Password);
                }

                if (!string.IsNullOrEmpty(user.UserRole))
                {
                    existing.UserRole = user.UserRole;
                    existing.RoleId = user.UserRole switch
                    {
                        "0" => 0,
                        "1" => 1,
                        "2" => 2,
                        _ => existing.RoleId // nan_T 2026-09-09：无法识别的角色不改原权限
                    };
                }

                if (!string.IsNullOrEmpty(user.IsActive))
                    existing.IsActive = user.IsActive;

                if (user.LastLoginTime != default)
                    existing.LastLoginTime = user.LastLoginTime;

                int result = await context.SaveChangesAsync();

                stopwatch.Stop();
                Debug.WriteLine($"{operation} 完成，用户ID: {user.Id}, 影响行数: {result}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 异常: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新用户最后登录时间
        /// </summary>
        public static async Task<int> UpdateLastLoginTimeAsync(string userName, DateTime lastLoginTime)
        {
            try
            {
                using var context = CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null) return 0;

                user.LastLoginTime = lastLoginTime;
                int result = await context.SaveChangesAsync();

                Debug.WriteLine($"更新用户最后登录时间成功，用户名: {userName}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新用户最后登录时间失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新用户密码
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="newPassword">明文新密码（nan_T 2026-09-09：方法内部统一做哈希，调用方不要再传哈希值）</param>
        public static async Task<int> UpdatePasswordAsync(string userName, string newPassword)
        {
            try
            {
                using var context = CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null) return 0;

                // nan_T 2026-09-09：入库前哈希，避免明文落库；已是哈希格式则原样保存（防重复哈希）
                user.Password = !string.IsNullOrEmpty(newPassword) && newPassword.StartsWith("PBKDF2:", StringComparison.Ordinal)
                    ? newPassword
                    : PasswordHasher.Hash(newPassword);
                int result = await context.SaveChangesAsync();

                Debug.WriteLine($"更新用户密码成功，用户名: {userName}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新用户密码失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新用户状态
        /// </summary>
        public static async Task<int> UpdateUserStatusAsync(string userName, string isActive)
        {
            try
            {
                using var context = CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null) return 0;

                user.IsActive = isActive;
                int result = await context.SaveChangesAsync();

                Debug.WriteLine($"更新用户状态成功，用户名: {userName}，状态: {isActive}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新用户状态失败: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region 删除

        /// <summary>
        /// 删除用户
        /// </summary>
        public static async Task<int> DeleteUserAsync(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            string operation = "删除用户";

            try
            {
                Debug.WriteLine($"{operation} 开始执行");

                using var context = CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null) return 0;

                context.Users.Remove(user);
                int result = await context.SaveChangesAsync();

                stopwatch.Stop();
                Debug.WriteLine($"{operation} 完成，用户ID: {id}, 影响行数: {result}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"{operation} 异常: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region 验证与统计

        /// <summary>
        /// 验证用户登录
        /// </summary>
        public static async Task<LoginSqlModel> ValidateUserLoginAsync(string userName, string password)
        {
            try
            {
                using var context = CreateDbContext();
                // nan_T 2026-09-09：密码为哈希存储，无法在 SQL 中比较，改为先按用户名查出再内存校验；
                // 启用状态同时兼容 "1" 与 "True" 两种历史写法。
                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.UserName == userName);

                bool passwordOk = user != null && PasswordHasher.Verify(password, user.Password).isValid;
                bool activeOk = user != null
                    && (user.IsActive == "1" || string.Equals(user.IsActive, "true", StringComparison.OrdinalIgnoreCase));

                if (user != null && passwordOk && activeOk)
                {
                    // 更新最后登录时间
                    user.LastLoginTime = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"验证用户登录失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        public static async Task<dynamic> GetUserStatisticsAsync()
        {
            try
            {
                using var context = CreateDbContext();
                var stats = await context.Users
                    .GroupBy(u => new { u.UserRole, u.IsActive })
                    .Select(g => new
                    {
                        UserRole = g.Key.UserRole,
                        IsActive = g.Key.IsActive,
                        Count = g.Count()
                    })
                    .OrderBy(s => s.UserRole).ThenBy(s => s.IsActive)
                    .ToListAsync();

                return stats;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取用户统计信息失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        public static async Task<string> TestConnectionAsync()
        {
            try
            {
                using var context = CreateDbContext();
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    return "连接成功";
                }
                else
                {
                    return "连接失败";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"数据库连接测试失败: {ex.Message}");
                return $"连接失败: {ex.Message}";
            }
        }

        #endregion
    }
}