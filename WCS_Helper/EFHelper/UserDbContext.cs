using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WCS_Models.LoginViewModel;

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
            switch (user.UserRole)
            {
                case "操作员":
                    user.RoleId = 1;
                    break;
                case "管理员":
                    user.RoleId = 0;
                    break;
                case "观察员":
                    user.RoleId = 2;
                    break;
                default:
                    user.RoleId = 0; // 默认值
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

                // 更新属性
                existing.UserName = user.UserName;
                existing.Password = user.Password;
                existing.UserRole = user.UserRole;
                existing.IsActive = user.IsActive;
                existing.LastLoginTime = user.LastLoginTime;
                existing.RoleId = user.RoleId;

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
        public static async Task<int> UpdatePasswordAsync(string userName, string newPassword)
        {
            try
            {
                using var context = CreateDbContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null) return 0;

                user.Password = newPassword;
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
                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.UserName == userName && u.Password == password && u.IsActive == "1");

                if (user != null)
                {
                    // 更新最后登录时间
                    user.LastLoginTime = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }

                return user;
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