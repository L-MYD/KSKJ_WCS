using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class UserRole
    {
        public int Id { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 管理权限名称
        /// </summary>
        public string RoleName { get; set; }
        /// <summary>
        /// 管理权限等级
        /// </summary>
        public int RoleId { get; set; }
    }

    public static class AuthManager
    {
        public static bool IsLoggedIn { get; private set; }
        public static string Username { get; private set; }

        public static bool Login(string username, string password)
        {
            // 模拟登录验证
            if (username == "admin" && password == "123456")
            {
                IsLoggedIn = true;
                Username = username;
                return true;
            }
            return false;
        }

        public static void Logout()
        {
            IsLoggedIn = false;
            Username = null;
        }
    }
}
