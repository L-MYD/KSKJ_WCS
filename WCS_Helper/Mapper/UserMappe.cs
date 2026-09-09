//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Dapper;
//using WCS_Models.LoginViewModel;
//using WCS_Models.SqlModel;

//namespace WCS_Helper.Mapper
//{
//    public class UserMapper
//    {
//        // 将连接字符串设为可配置
//        private static readonly string ConnectionString = ConfigurationHelper.GetConnectionString();

//        // 添加一个配置构造函数（可选）
//        public static void Configure(string connectionString)
//        {
//            if (!string.IsNullOrEmpty(connectionString))
//            {
//                // 这里可以根据需要更新连接字符串
//            }
//        }

//        /// <summary>
//        /// 根据用户名查询用户
//        /// </summary>
//        /// <param name="userName">用户名</param>
//        /// <returns>用户信息</returns>
//        public static async Task<LoginSqlModel> FindByUserNameAsync(string userName)
//        {
//            var stopwatch = Stopwatch.StartNew();
//            string operation = "根据用户名查询用户";
//            string sql = "SELECT * FROM UserRole WHERE username = @username";

//            try
//            {
//                // 记录SQL操作开始
//                Debug.WriteLine($"{operation} 开始执行，SQL: {sql}");

//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    // 异步打开连接
//                    await connection.OpenAsync();

//                    // 异步执行查询
//                    var result = await connection.QueryFirstOrDefaultAsync<LoginSqlModel>(sql, new { username = userName });

//                    stopwatch.Stop();

//                    if (result != null)
//                    {
//                        // 记录成功和结果
//                        Debug.WriteLine($"{operation} 成功，耗时: {stopwatch.ElapsedMilliseconds}ms");
//                        Debug.WriteLine($"{operation} 查询到1条记录");
//                    }
//                    else
//                    {
//                        // 记录查询成功但无结果
//                        Debug.WriteLine($"{operation} 成功，耗时: {stopwatch.ElapsedMilliseconds}ms");
//                        Debug.WriteLine($"{operation} 未查询到记录");
//                    }

//                    return result;
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} SQL Server错误: {sqlEx.Number} - {sqlEx.Message}");
//                return null;
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} 异常: {ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// 根据用户ID查询用户信息
//        /// </summary>
//        /// <param name="id">用户ID</param>
//        /// <returns>用户信息</returns>
//        public static async Task<LoginSqlModel> FindByIdAsync(int id)
//        {
//            var stopwatch = Stopwatch.StartNew();
//            string operation = "根据用户ID查询用户";
//            string sql = "SELECT * FROM wcs_userinfo WHERE id = @id";

//            try
//            {
//                Debug.WriteLine($"{operation} 开始执行，SQL: {sql}");

//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.QueryFirstOrDefaultAsync<LoginSqlModel>(sql, new { id });

//                    stopwatch.Stop();
//                    Debug.WriteLine($"{operation} 完成，耗时: {stopwatch.ElapsedMilliseconds}ms");

//                    return result;
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} SQL Server错误: {sqlEx.Number} - {sqlEx.Message}");
//                return null;
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} 异常: {ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// 新增用户
//        /// </summary>
//        /// <param name="user">用户信息</param>
//        /// <returns>新增的用户ID</returns>
//        public static async Task<int> InsertUserAsync(LoginSqlModel user)
//        {
//            user.lastlogintime = DateTime.UtcNow.ToString();
//            switch (user.userrole)
//            {
//                case "操作员":user.roleid = 1; break;
//                case "管理员": user.roleid = 0; break;
//                case "观察员": user.roleid = 2; break;
//            }
//            var stopwatch = Stopwatch.StartNew();
//            string operation = "新增用户";
//            string sql = @"
//            INSERT INTO UserRole (
//                UserName, Password, RoleName, IsActive, CreatedTime, RoleId
//            ) VALUES (
//                @username, @password, @userrole, @isactive, @lastlogintime, @roleid
//            );
//            SELECT CAST(SCOPE_IDENTITY() AS INT);";

//            try
//            {
//                Debug.WriteLine($"{operation} 开始执行，SQL: {sql}");

//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();

//                    // 执行插入并获取新插入的ID
//                    var userId = await connection.ExecuteScalarAsync<int>(sql, user);

//                    stopwatch.Stop();
//                    Debug.WriteLine($"{operation} 成功，用户名: {user.username}, 新ID: {userId}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

//                    return userId;
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} SQL Server错误: {sqlEx.Number} - {sqlEx.Message}");

//                // 处理唯一约束冲突（用户名重复）
//                if (sqlEx.Number == 2627 || sqlEx.Number == 2601) // 唯一约束冲突错误代码
//                {
//                    throw new Exception($"用户名 '{user.username}' 已存在", sqlEx);
//                }
//                return 0;
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} 异常: {ex.Message}");
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 更新用户信息
//        /// </summary>
//        /// <param name="user">用户信息</param>
//        /// <returns>影响的行数</returns>
//        public static async Task<int> UpdateUserAsync(LoginSqlModel user)
//        {
//            var stopwatch = Stopwatch.StartNew();
//            string operation = "更新用户信息";
//            string sql = @"
//            UPDATE wcs_userinfo SET 
//                username = @username,
//                password = @password,
//                userrole = @userrole,
//                isactive = @isactive,
//                lastlogintime = @lastlogintime
//            WHERE id = @id";

//            try
//            {
//                Debug.WriteLine($"{operation} 开始执行，SQL: {sql}");

//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.ExecuteAsync(sql, user);

//                    stopwatch.Stop();
//                    Debug.WriteLine($"{operation} 完成，用户ID: {user.id}, 影响行数: {result}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

//                    return result;
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} SQL Server错误: {sqlEx.Number} - {sqlEx.Message}");
//                return 0;
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} 异常: {ex.Message}");
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 删除用户
//        /// </summary>
//        /// <param name="id">用户ID</param>
//        /// <returns>影响的行数</returns>
//        public static async Task<int> DeleteUserAsync(int id)
//        {
//            var stopwatch = Stopwatch.StartNew();
//            string operation = "删除用户";
//            string sql = "DELETE FROM wcs_userinfo WHERE id = @id";

//            try
//            {
//                Debug.WriteLine($"{operation} 开始执行，SQL: {sql}");

//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.ExecuteAsync(sql, new { id });

//                    stopwatch.Stop();
//                    Debug.WriteLine($"{operation} 完成，用户ID: {id}, 影响行数: {result}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

//                    return result;
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} SQL Server错误: {sqlEx.Number} - {sqlEx.Message}");
//                return 0;
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} 异常: {ex.Message}");
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 获取所有用户列表
//        /// </summary>
//        /// <returns>用户列表</returns>
//        public static async Task<List<LoginSqlModel>> GetAllUsersAsync()
//        {
//            var stopwatch = Stopwatch.StartNew();
//            string operation = "获取所有用户列表";
//            string sql = "SELECT * FROM wcs_userinfo ORDER BY id";

//            try
//            {
//                Debug.WriteLine($"{operation} 开始执行，SQL: {sql}");

//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = (await connection.QueryAsync<LoginSqlModel>(sql)).ToList();

//                    stopwatch.Stop();
//                    Debug.WriteLine($"{operation} 完成，获取到 {result.Count} 条记录，耗时: {stopwatch.ElapsedMilliseconds}ms");

//                    return result;
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} SQL Server错误: {sqlEx.Number} - {sqlEx.Message}");
//                return new List<LoginSqlModel>();
//            }
//            catch (Exception ex)
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"{operation} 异常: {ex.Message}");
//                return new List<LoginSqlModel>();
//            }
//        }

//        /// <summary>
//        /// 根据角色查询用户
//        /// </summary>
//        /// <param name="userrole">用户角色</param>
//        /// <returns>用户列表</returns>
//        public static async Task<List<LoginSqlModel>> GetUsersByRoleAsync(string userrole)
//        {
//            string sql = "SELECT * FROM wcs_userinfo WHERE userrole = @userrole ORDER BY id";

//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.QueryAsync<LoginSqlModel>(sql, new { userrole });
//                    return result.ToList();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"根据角色查询用户失败: {ex.Message}");
//                return new List<LoginSqlModel>();
//            }
//        }

//        /// <summary>
//        /// 更新用户最后登录时间
//        /// </summary>
//        /// <param name="username">用户名</param>
//        /// <param name="lastLoginTime">最后登录时间</param>
//        /// <returns>影响的行数</returns>
//        public static async Task<int> UpdateLastLoginTimeAsync(string username, string lastLoginTime)
//        {
//            string sql = "UPDATE wcs_userinfo SET lastlogintime = @lastlogintime WHERE username = @username";

//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.ExecuteAsync(sql, new { username, lastlogintime = lastLoginTime });
//                    Debug.WriteLine($"更新用户最后登录时间成功，用户名: {username}");
//                    return result;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"更新用户最后登录时间失败: {ex.Message}");
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 更新用户密码
//        /// </summary>
//        /// <param name="username">用户名</param>
//        /// <param name="newPassword">新密码</param>
//        /// <returns>影响的行数</returns>
//        public static async Task<int> UpdatePasswordAsync(string username, string newPassword)
//        {
//            string sql = "UPDATE wcs_userinfo SET password = @password WHERE username = @username";

//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.ExecuteAsync(sql, new { username, password = newPassword });
//                    Debug.WriteLine($"更新用户密码成功，用户名: {username}");
//                    return result;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"更新用户密码失败: {ex.Message}");
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 更新用户状态
//        /// </summary>
//        /// <param name="username">用户名</param>
//        /// <param name="isActive">是否激活</param>
//        /// <returns>影响的行数</returns>
//        public static async Task<int> UpdateUserStatusAsync(string username, string isActive)
//        {
//            string sql = "UPDATE wcs_userinfo SET isactive = @isactive WHERE username = @username";

//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.ExecuteAsync(sql, new { username, isactive = isActive });
//                    Debug.WriteLine($"更新用户状态成功，用户名: {username}，状态: {isActive}");
//                    return result;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"更新用户状态失败: {ex.Message}");
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 分页查询用户列表
//        /// </summary>
//        /// <param name="pageIndex">页码</param>
//        /// <param name="pageSize">每页大小</param>
//        /// <param name="keyword">关键词（可选）</param>
//        /// <returns>分页数据和总数</returns>
//        public static async Task<(List<LoginSqlModel> Data, int TotalCount)> GetPagedUsersAsync(int pageIndex, int pageSize, string keyword = null)
//        {
//            using (var connection = new SqlConnection(ConnectionString))
//            {
//                try
//                {
//                    await connection.OpenAsync();

//                    string whereSql = "WHERE 1=1";
//                    var parameters = new DynamicParameters();

//                    if (!string.IsNullOrEmpty(keyword))
//                    {
//                        whereSql += " AND (username LIKE '%' + @keyword + '%' OR userrole LIKE '%' + @keyword + '%')";
//                        parameters.Add("keyword", keyword);
//                    }

//                    // 查询总数
//                    string countSql = $"SELECT COUNT(1) FROM wcs_userinfo {whereSql}";
//                    int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

//                    // 查询分页数据
//                    string dataSql = $@"
//                    SELECT * FROM wcs_userinfo 
//                    {whereSql} 
//                    ORDER BY id 
//                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

//                    parameters.Add("offset", (pageIndex - 1) * pageSize);
//                    parameters.Add("pageSize", pageSize);

//                    var data = (await connection.QueryAsync<LoginSqlModel>(dataSql, parameters)).ToList();

//                    return (data, totalCount);
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"分页查询用户失败: {ex.Message}");
//                    return (new List<LoginSqlModel>(), 0);
//                }
//            }
//        }

//        /// <summary>
//        /// 获取用户统计信息
//        /// </summary>
//        /// <returns>用户统计信息</returns>
//        public static async Task<dynamic> GetUserStatisticsAsync()
//        {
//            string sql = @"
//            SELECT 
//                userrole,
//                isactive,
//                COUNT(*) as count
//            FROM wcs_userinfo 
//            GROUP BY userrole, isactive
//            ORDER BY userrole, isactive";

//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    return (await connection.QueryAsync(sql)).ToList();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"获取用户统计信息失败: {ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// 验证用户登录
//        /// </summary>
//        /// <param name="username">用户名</param>
//        /// <param name="password">密码</param>
//        /// <returns>用户信息</returns>
//        public static async Task<LoginSqlModel> ValidateUserLoginAsync(string username, string password)
//        {
//            string sql = "SELECT * FROM wcs_userinfo WHERE username = @username AND password = @password AND isactive = '1'";

//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    var result = await connection.QueryFirstOrDefaultAsync<LoginSqlModel>(sql, new { username, password });

//                    if (result != null)
//                    {
//                        // 更新最后登录时间
//                        await UpdateLastLoginTimeAsync(username, DateTime.UtcNow);
//                    }

//                    return result;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"验证用户登录失败: {ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// 获取数据库连接测试
//        /// </summary>
//        /// <returns>数据库版本信息</returns>
//        public static async Task<string> TestConnectionAsync()
//        {
//            try
//            {
//                using (var connection = new SqlConnection(ConnectionString))
//                {
//                    await connection.OpenAsync();
//                    return await connection.ExecuteScalarAsync<string>("SELECT @@VERSION");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"数据库连接测试失败: {ex.Message}");
//                return $"连接失败: {ex.Message}";
//            }
//        }
//    }
//}