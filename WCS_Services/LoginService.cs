using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WCS_Models.LoginViewModel;
using WCS_Helper.Mapper; // 根据您的项目结构调整这个命名空间
using WCS_Helper; // nan_T 2026-09-09：引入 PasswordHasher 密码哈希工具

using WCS_IServices;
namespace WCS_Services // 根据您的项目结构调整这个命名空间
{
    public class LoginService : ILoginService
    {
        private readonly ILogger<LoginService> _logger;

        public LoginService(ILogger<LoginService> logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 查询数据库的用户名和密码验证
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>用户信息或null</returns>
        public async Task<LoginSqlModel> ValidateUserCredentials(string username, string password)
        {
            try
            {
                // 验证输入参数
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger?.LogWarning("用户名或密码为空");
                    return null;
                }

                var user = await UserDbContext.FindByUserNameAsync(username);

                // nan_T 2026-09-09：密码改为 PBKDF2 哈希校验，兼容旧明文（needsUpgrade=true 表示库中还是明文）
                var verifyResult = user != null
                    ? PasswordHasher.Verify(password, user.Password)
                    : (isValid: false, needsUpgrade: false);

                // 验证用户是否存在且密码正确
                if (user != null && verifyResult.isValid)
                {
                    // nan_T 2026-09-09：修复启用状态判断不一致问题。
                    // 原来判断 IsActive != "True"，但新增用户默认写入 "1"，导致新用户永远无法登录；
                    // 现统一为 "1" 或 "true"（忽略大小写）视为启用，其余视为禁用。
                    if (!IsUserActive(user.IsActive))
                    {
                        _logger?.LogWarning($"用户 {username} 已被禁用");
                        return null;
                    }

                    // nan_T 2026-09-09：旧明文密码登录成功后自动升级为哈希存储，失败不影响本次登录
                    if (verifyResult.needsUpgrade)
                    {
                        try
                        {
                            await UserDbContext.UpdatePasswordAsync(username, PasswordHasher.Hash(password));
                            _logger?.LogInformation($"用户 {username} 的明文密码已自动升级为哈希存储");
                        }
                        catch (Exception upgradeEx)
                        {
                            _logger?.LogWarning(upgradeEx, $"用户 {username} 密码哈希升级失败，下次登录将重试");
                        }
                    }

                    _logger?.LogInformation($"用户 {username} 登录成功");
                    return user;
                }
                else
                {
                    _logger?.LogWarning($"用户 {username} 登录失败：用户不存在或密码错误");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"验证用户 {username} 凭证时发生异常");
                throw; // 或者返回null，根据您的需求
            }
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>新增的用户ID</returns>
        public async Task<int> InsertUserCredentials(LoginSqlModel user)
        {
            try
            {
                // 验证输入参数
                if (user == null)
                {
                    _logger?.LogWarning("插入用户失败：用户信息为空");
                    return 0;
                }

                if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
                {
                    _logger?.LogWarning("插入用户失败：用户名或密码为空");
                    return 0;
                }

                // 检查用户名是否已存在
                var existingUser = await UserDbContext.FindByUserNameAsync(user.UserName);
                if (existingUser != null)
                {
                    _logger?.LogWarning($"插入用户失败：用户名 {user.UserName} 已存在");
                    return -1; // 返回-1表示用户名已存在
                }

                // 设置默认值
                user.IsActive = string.IsNullOrEmpty(user.IsActive) ? "1" : user.IsActive;
                user.UserRole = string.IsNullOrEmpty(user.UserRole) ? "2" : user.UserRole; // 默认角色：观察员

                // nan_T 2026-09-09：入库前将明文密码哈希，数据库不再保存明文密码
                user.Password = PasswordHasher.Hash(user.Password);

                int result = await UserDbContext.InsertUserAsync(user);

                if (result > 0)
                {
                    _logger?.LogInformation($"用户 {user.UserName} 插入成功，ID: {result}");
                }
                else
                {
                    _logger?.LogWarning($"用户 {user.UserName} 插入失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"插入用户 {user?.UserName} 时发生异常");
                throw; // 或者返回0，根据您的需求
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>更新后的用户信息</returns>
        public async Task<LoginSqlModel> UpdateUserAsync(LoginSqlModel user)
        {
            try
            {
                // 验证输入参数
                if (user == null || user.Id <= 0)
                {
                    _logger?.LogWarning("更新用户失败：用户信息无效");
                    return null;
                }

                // 检查用户是否存在
                var existingUser = await UserDbContext.FindByIdAsync(user.Id);
                if (existingUser == null)
                {
                    _logger?.LogWarning($"更新用户失败：用户ID {user.Id} 不存在");
                    return null;
                }

                int result = await UserDbContext.UpdateUserAsync(user);

                if (result > 0)
                {
                    _logger?.LogInformation($"用户ID {user.Id} 更新成功");
                    return await UserDbContext.FindByIdAsync(user.Id);
                }
                else
                {
                    _logger?.LogWarning($"用户ID {user.Id} 更新失败");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"更新用户ID {user?.Id} 时发生异常");
                throw; // 或者返回null，根据您的需求
            }
        }

        /// <summary>
        /// 获取所有用户列表
        /// </summary>
        /// <returns>用户列表</returns>
        public async Task<List<LoginSqlModel>> GetUserList()
        {
            try
            {
                var userList = await UserDbContext.GetAllUsersAsync();
                _logger?.LogInformation($"获取用户列表成功，共 {userList?.Count ?? 0} 个用户");
                return userList;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取用户列表时发生异常");
                throw; // 或者返回空列表，根据您的需求
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>删除结果（大于0表示成功）</returns>
        public async Task<int> DeleteUser(int userId)
        {
            try
            {
                // 验证输入参数
                if (userId <= 0)
                {
                    _logger?.LogWarning("删除用户失败：用户ID无效");
                    return -1;
                }

                // 检查用户是否存在
                var existingUser = await UserDbContext.FindByIdAsync(userId);
                if (existingUser == null)
                {
                    _logger?.LogWarning($"删除用户失败：用户ID {userId} 不存在");
                    return -1;
                }

                int result = await UserDbContext.DeleteUserAsync(userId);

                if (result > 0)
                {
                    _logger?.LogInformation($"用户ID {userId} 删除成功");
                }
                else
                {
                    _logger?.LogWarning($"用户ID {userId} 删除失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"删除用户ID {userId} 时发生异常");
                throw; // 或者返回-1，根据您的需求
            }
        }

        /// <summary>
        /// 根据用户ID获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        public async Task<LoginSqlModel> GetUserById(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return null;
                }

                return await UserDbContext.FindByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"获取用户ID {userId} 信息时发生异常");
                throw; // 或者返回null
            }
        }

        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户信息</returns>
        public async Task<LoginSqlModel> GetUserByUsername(string username)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    return null;
                }

                return await UserDbContext.FindByUserNameAsync(username);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"获取用户名 {username} 信息时发生异常");
                throw; // 或者返回null
            }
        }

        /// <summary>
        /// 更新用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码</param>
        /// <param name="oldPassword">旧密码（用于验证）</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdatePassword(int userId, string newPassword, string oldPassword = null)
        {
            try
            {
                if (userId <= 0 || string.IsNullOrEmpty(newPassword))
                {
                    return false;
                }

                var user = await UserDbContext.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // nan_T 2026-09-09：旧密码校验同样走哈希校验（兼容旧明文），不再明文比较
                // 如果需要验证旧密码
                if (!string.IsNullOrEmpty(oldPassword))
                {
                    var oldVerify = PasswordHasher.Verify(oldPassword, user.Password);
                    if (!oldVerify.isValid)
                    {
                        return false;
                    }
                }

                // nan_T 2026-09-09：新密码先哈希再落库
                user.Password = PasswordHasher.Hash(newPassword);
                var result = await UserDbContext.UpdateUserAsync(user);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"更新用户ID {userId} 密码时发生异常");
                throw; // 或者返回false
            }
        }

        /// <summary>
        /// nan_T 2026-09-09：统一判断用户是否启用。
        /// 数据库中历史数据存在 "1"、"True" 两种写法，统一视为启用；
        /// "0"、"False"、空值等一律视为禁用，避免各处判断口径不一致。
        /// </summary>
        private static bool IsUserActive(string isActive)
        {
            if (string.IsNullOrWhiteSpace(isActive))
                return false;

            return isActive == "1"
                || string.Equals(isActive, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}