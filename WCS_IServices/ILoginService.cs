using System.Collections.Generic;
using System.Threading.Tasks;
using WCS_Models.LoginViewModel;

namespace WCS_IServices
{
    /// <summary>
    /// 登录/用户管理服务接口。
    /// 由 WCS_Services 层的 LoginService 实现，供 LoginController 面向接口调用。
    /// </summary>
    public interface ILoginService
    {
        /// <summary>校验用户名与密码，返回匹配的用户实体</summary>
        Task<LoginSqlModel> ValidateUserCredentials(string username, string password);

        /// <summary>新增用户，返回受影响行数/新用户标识</summary>
        Task<int> InsertUserCredentials(LoginSqlModel user);

        /// <summary>更新用户信息</summary>
        Task<LoginSqlModel> UpdateUserAsync(LoginSqlModel user);

        /// <summary>获取全部用户列表</summary>
        Task<List<LoginSqlModel>> GetUserList();

        /// <summary>按用户Id删除</summary>
        Task<int> DeleteUser(int userId);

        /// <summary>按用户Id查询</summary>
        Task<LoginSqlModel> GetUserById(int userId);

        /// <summary>按用户名查询</summary>
        Task<LoginSqlModel> GetUserByUsername(string username);

        /// <summary>修改密码（可选校验旧密码）</summary>
        Task<bool> UpdatePassword(int userId, string newPassword, string oldPassword = null);
    }
}
