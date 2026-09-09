using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.LoginViewModel
{
    public class LoginSqlModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserRole { get; set; } //权限（0-管理员，1-操作员，2-观察员）
        public string IsActive { get; set; } //是否启用，0表示禁用，1表示启动
        public DateTime LastLoginTime { get; set; } //最后登录时间
        public int RoleId { get; set; }
    }

    /// <summary>
    /// 未登录时的登录模型
    /// </summary>
    public class Login
    {
        public string username { get; set; }
        public string password { get; set; }
        public bool rememberme { get; set; }
    }

    /// <summary>
    /// API 统一响应格式
    /// </summary>
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T data { get; set; }

        public ApiResponse(int code, string message, T dat)
        {
            Code = code;
            Message = message;
            data = dat;
        }

        // 成功响应快捷方法
        public static ApiResponse<T> Success(T data)
        {
            return new ApiResponse<T>(200, "Success", data);
        }
        // 错误响应快捷方法
        public static ApiResponse<T> Error(string message, int code = 500)
        {
            return new ApiResponse<T>(code, message, default(T));
        }
    }

    public class QueryModel<T>
    {
        public List<T> list { get; set; }
        public int total { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
    }

}
