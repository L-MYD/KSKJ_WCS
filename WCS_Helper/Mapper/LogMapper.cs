//using WCS_Models.SqlModel;
//using WCS_Models.WCSModel;
//using WCS_Models.WCSModel.LogModel;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Dapper;
//using Npgsql;

//namespace WCS_Helper.Mapper
//{
//    public class LogsDbContext
//    {
//        private static readonly string SqlConn = ConfigurationHelper.GetConnectionString();
//        public static bool Insert(LogModel log)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = @"
//                    INSERT INTO Logs 
//                    ( UserType, LogType, Level, Message, Module, Operation, Details, UserId, IpAddress, CreateTime, IsArchived) 
//                    VALUES 
//                    ( @UserType, @LogType, @Level, @Message, @Module, @Operation, @Details, @UserId, @IpAddress, @CreateTime, @IsArchived)";

//                    int result = connection.Execute(sql, log);
//                    return result > 0;
//                }
//                catch (Exception ex)
//                {
//                    // 这里可以记录错误日志
//                    SaveErrToText("插入日志失败: ",$"{ex.Message}");
//                    return false;
//                }
//            }
//        }

//        //public static bool Insert(LogModel log)
//        //{
//        //    using (var connection = new NpgsqlConnection(SqlConn))
//        //    {
//        //        try
//        //        {
//        //            string sql = @"
//        //            INSERT INTO Logs 
//        //            ( UserType, LogType, Level, Message, Module, Operation, Details, UserId, IpAddress, CreateTime, IsArchived) 
//        //            VALUES 
//        //            ( @UserType, @LogType, @Level, @Message, @Module, @Operation, @Details, @UserId, @IpAddress, @CreateTime, @IsArchived)";

//        //            int result = connection.Execute(sql, log);
//        //            return result > 0;
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            // 这里可以记录错误日志
//        //            SaveErrToText("插入日志失败: ", $"{ex.Message}");
//        //            return false;
//        //        }
//        //    }
//        //}

//        // 批量插入日志
//        public static bool InsertBatch(List<LogModel> logs)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = @"
//                    INSERT INTO SystemLogs 
//                    (id, UserType, LogType, Level, Message, Module, Operation, Details, UserId, IpAddress, CreateTime, IsArchived) 
//                    VALUES 
//                    (@id, @UserType, @LogType, @Level, @Message, @Module, @Operation, @Details, @UserId, @IpAddress, @CreateTime, @IsArchived)";

//                    int result = connection.Execute(sql, logs);
//                    return result == logs.Count;
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"批量插入日志失败: ", $"{ex.Message}");
//                    return false;
//                }
//            }
//        }

//        // 根据ID查询日志
//        public static LogModel GetById(int id)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = "SELECT * FROM SystemLogs WHERE id = @id";
//                    return connection.QueryFirstOrDefault<LogModel>(sql, new { id = id });
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"查询日志失败: ", $"{ex.Message}");
//                    return null;
//                }
//            }
//        }

//        // 查询所有日志
//        public static List<LogModel> GetAll()
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = "SELECT * FROM SystemLogs ORDER BY CreateTime DESC";
//                    return connection.Query<LogModel>(sql).ToList();
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"查询所有日志失败: ", $"{ex.Message}");
//                    return new List<LogModel>();
//                }
//            }
//        }

//        // 根据条件查询日志
//        public static List<LogModel> GetByCondition(string level = null, string module = null, string userId = null, DateTime? startDate = null, DateTime? endDate = null)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = "SELECT * FROM SystemLogs WHERE 1=1";
//                    var parameters = new DynamicParameters();

//                    if (!string.IsNullOrEmpty(level))
//                    {
//                        sql += " AND Level = @Level";
//                        parameters.Add("Level", level);
//                    }

//                    if (!string.IsNullOrEmpty(module))
//                    {
//                        sql += " AND Module = @Module";
//                        parameters.Add("Module", module);
//                    }

//                    if (!string.IsNullOrEmpty(userId))
//                    {
//                        sql += " AND UserId = @UserId";
//                        parameters.Add("UserId", userId);
//                    }

//                    if (startDate.HasValue)
//                    {
//                        sql += " AND CreateTime >= @StartDate";
//                        parameters.Add("StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
//                    }

//                    if (endDate.HasValue)
//                    {
//                        sql += " AND CreateTime <= @EndDate";
//                        parameters.Add("EndDate", endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
//                    }

//                    sql += " ORDER BY CreateTime DESC";

//                    return connection.Query<LogModel>(sql, parameters).ToList();
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"条件查询日志失败: ", $"{ex.Message}");
//                    return new List<LogModel>();
//                }
//            }
//        }

//        // 分页查询日志
//        public static (List<LogModel> Data, int TotalCount) GetByPage(int pageIndex, int pageSize, string level = null, string module = null)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string whereSql = "WHERE 1=1";
//                    var parameters = new DynamicParameters();

//                    if (!string.IsNullOrEmpty(level))
//                    {
//                        whereSql += " AND Level = @Level";
//                        parameters.Add("Level", level);
//                    }

//                    if (!string.IsNullOrEmpty(module))
//                    {
//                        whereSql += " AND Module = @Module";
//                        parameters.Add("Module", module);
//                    }

//                    // 查询总数
//                    string countSql = $"SELECT COUNT(1) FROM SystemLogs {whereSql}";
//                    int totalCount = connection.ExecuteScalar<int>(countSql, parameters);

//                    // 查询分页数据
//                    string dataSql = $@"
//                    SELECT * FROM SystemLogs 
//                    {whereSql} 
//                    ORDER BY CreateTime DESC 
//                    OFFSET {(pageIndex - 1) * pageSize} ROWS 
//                    FETCH NEXT {pageSize} ROWS ONLY";

//                    var data = connection.Query<LogModel>(dataSql, parameters).ToList();

//                    return (data, totalCount);
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"分页查询日志失败: ",$"{ex.Message}");
//                    return (new List<LogModel>(), 0);
//                }
//            }
//        }

//        // 更新日志
//        public static bool Update(LogModel log)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = @"
//                    UPDATE SystemLogs SET 
//                    UserType = @UserType, 
//                    LogType = @LogType, 
//                    Level = @Level, 
//                    Message = @Message, 
//                    Module = @Module, 
//                    Operation = @Operation, 
//                    Details = @Details, 
//                    UserId = @UserId, 
//                    IpAddress = @IpAddress, 
//                    CreateTime = @CreateTime, 
//                    IsArchived = @IsArchived 
//                    WHERE id = @id";

//                    int result = connection.Execute(sql, log);
//                    return result > 0;
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"更新日志失败: ", $"{ex.Message}");
//                    return false;
//                }
//            }
//        }

//        // 根据ID删除日志
//        public static bool Delete(int id)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = "DELETE FROM SystemLogs WHERE id = @id";
//                    int result = connection.Execute(sql, new { id = id });
//                    return result > 0;
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"删除日志失败: ", $"{ex.Message}");
//                    return false;
//                }
//            }
//        }

//        // 批量删除日志
//        public static bool DeleteBatch(List<int> ids)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = "DELETE FROM SystemLogs WHERE id IN @Ids";
//                    int result = connection.Execute(sql, new { Ids = ids });
//                    return result > 0;
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"批量删除日志失败: ", $"{ex.Message}");
//                    return false;
//                }
//            }
//        }

//        // 归档日志
//        public static bool ArchiveLogs(DateTime beforeDate)
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = "UPDATE SystemLogs SET IsArchived = 'True' WHERE CreateTime <= @BeforeDate AND IsArchived = 'False'";
//                    int result = connection.Execute(sql, new { BeforeDate = beforeDate.ToString("yyyy-MM-dd HH:mm:ss") });
//                    return result > 0;
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"归档日志失败: ", $"{ex.Message}");
//                    return false;
//                }
//            }
//        }

//        // 获取日志统计信息
//        public static dynamic GetLogStatistics()
//        {
//            using (var connection = new SqlConnection(SqlConn))
//            {
//                try
//                {
//                    string sql = @"
//                    SELECT 
//                        Level,
//                        COUNT(*) as Count
//                    FROM SystemLogs 
//                    GROUP BY Level
//                    ORDER BY Count DESC";

//                    return connection.Query(sql).ToList();
//                }
//                catch (Exception ex)
//                {
//                    SaveErrToText($"获取日志统计失败: ", $"{ex.Message}");
//                    return null;
//                }
//            }

//        }


//        /// <summary>
//        /// 将异常信息保存至文本文件
//        /// </summary>
//        /// <param name="Detail"></param>
//        /// <param name="reason"></param>
//        public static void SaveErrToText(string Detail, string reason)
//        {
//            const string FilePath = @"C:\LogErrLog";
//            var FileName = DateTime.UtcNow.ToString("yyyyMMdd");

//            StreamWriter sw = null;
//            FileStream fs = null;

//            try
//            {
//                if (!Directory.Exists(FilePath))
//                {
//                    Directory.CreateDirectory(FilePath);
//                }

//                FileName = "LogErrLog_" + FileName + ".txt";

//                var Path = FilePath + @"\" + FileName;

//                if (!File.Exists(Path))
//                {
//                    fs = File.Create(Path);

//                    fs.Close();
//                    fs.Dispose();
//                }

//                sw = File.AppendText(Path);
//                var AddTxt =
//                    $"DealMessage:{Detail} \n Reason:{reason} \n Datetime:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} \n";

//                sw.Write(AddTxt);
//                sw.Close();
//            }
//            catch
//            {
//                sw?.Close();

//                fs?.Close();
//            }
//        }

//    }
//}