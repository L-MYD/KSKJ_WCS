using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

//namespace WCS_Helper
//{
//    /// <summary>
//    /// wcs数据库连接
//    /// </summary>
//    public static class WcsSQLServer
//    {
//        private static readonly string SqlConn = ConfigurationHelper.GetConnectionString();//获取数据库连接字符串

//        /// <summary>
//        /// 执行查询，返回是否有结果
//        /// </summary>
//        /// <param name="queryString"></param>
//        /// <returns></returns>
//        public static bool SelectResult(string queryString)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var adapter = new SqlDataAdapter
//                    {
//                        SelectCommand = new SqlCommand(queryString, connection)
//                    };

//                    var dataTable = new DataTable();
//                    adapter.Fill(dataTable);

//                    return dataTable.Rows.Count != 0;
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return false;
//            }
//        }

//        /// <summary>
//        /// 执行存储过程
//        /// </summary>
//        /// <param name="SPORCName"></param>
//        public static void ExecuteProcedure(string SPORCName)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var comm = new SqlCommand(SPORCName, connection)
//                    {
//                        CommandType = CommandType.StoredProcedure
//                    };

//                    connection.Open();
//                    comm.ExecuteNonQuery();
//                    connection.Close();
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(SPORCName, e.Message);
//            }
//        }

//        /// <summary>
//        /// 执行查询并返回 DataTable
//        /// </summary>
//        /// <param name="queryString"></param>
//        /// <returns></returns>
//        public static DataTable SelectContentToDataTable(string queryString)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var adapter = new SqlDataAdapter
//                    {
//                        SelectCommand = new SqlCommand(queryString, connection)
//                    };
//                    var dataTable = new DataTable();
//                    adapter.Fill(dataTable);

//                    return dataTable;
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return null;
//            }
//        }

//        /// <summary>
//        /// 执行查询并返回泛型列表
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="queryString"></param>
//        /// <returns></returns>
//        public static List<T> SelectContentToDataTable<T>(string queryString)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var adapter = new SqlDataAdapter
//                    {
//                        SelectCommand = new SqlCommand(queryString, connection)
//                    };
//                    var dataTable = new DataTable();
//                    adapter.Fill(dataTable);

//                    if (dataTable != null && dataTable.Rows.Count > 0)
//                    {
//                        return ConvertToModel<T>(dataTable);
//                    }
//                    else
//                    {
//                        return null;
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return null;
//            }
//        }

//        /// <summary>
//        /// 将 DataTable 转换为泛型列表
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="dt"></param>
//        /// <returns></returns>
//        private static List<T> ConvertToModel<T>(DataTable dt)
//        {
//            var _ReturnList = new List<T>();

//            foreach (DataRow dr in dt.Rows)
//            {
//                var columns = dr.Table.Columns;

//                var t = Activator.CreateInstance<T>();

//                var pi = t.GetType().GetProperties();

//                foreach (var p in pi)
//                {
//                    string Name = p.Name,
//                        _Name = Name.ToLower();

//                    foreach (var c in columns)
//                    {
//                        string FieldName = c.ToString(),
//                            _FieldName = FieldName.ToLower();

//                        if (_FieldName != _Name) continue;

//                        p.SetValue(t, Convert.ChangeType(dr[FieldName], p.PropertyType), null);

//                        break;
//                    }
//                }

//                _ReturnList.Add(t);
//            }

//            return _ReturnList;
//        }

//        /// <summary>
//        /// 根据表格名字查询表格信息
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="TableName">表格名字</param>
//        /// <returns></returns>
//        public static List<T> SelectContent<T>(string TableName)
//        {
//            var queryString = $"SELECT * FROM {TableName} WITH(NOLOCK);";

//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var adapter = new SqlDataAdapter
//                    {
//                        SelectCommand = new SqlCommand(queryString, connection)
//                    };
//                    var dataTable = new DataTable();
//                    adapter.Fill(dataTable);

//                    return dataTable.Rows.Count > 0 ? ConvertToModel<T>(dataTable) : new List<T>();
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return new List<T>();
//            }
//        }

//        /// <summary>
//        /// 查询数据并返回泛型列表
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="queryString"></param>
//        /// <returns></returns>
//        public static List<T> QueryContent<T>(string queryString)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var adapter = new SqlDataAdapter
//                    {
//                        SelectCommand = new SqlCommand(queryString, connection)
//                    };
//                    var dataTable = new DataTable();
//                    adapter.Fill(dataTable);

//                    return dataTable.Rows.Count > 0 ? ConvertToModel<T>(dataTable) : new List<T>();
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return new List<T>();
//            }
//        }

//        /// <summary>
//        /// 执行插入操作
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="TableName"></param>
//        /// <param name="Data"></param>
//        /// <returns></returns>
//        public static int ExecuteInsert<T>(string TableName, T Data)
//        {
//            var queryString = string.Empty;

//            try
//            {
//                string Fields = string.Empty,
//                    Values = string.Empty;

//                var pi = Data.GetType().GetProperties();

//                foreach (var p in pi)
//                {
//                    string Field = p.Name,
//                           Value = $"'{p.GetValue(Data)}'";

//                    Fields = Fields + Field + ",";
//                    Values = Values + Value + ",";
//                }

//                Fields = Fields.TrimEnd(',');
//                Values = Values.TrimEnd(',');

//                queryString = $"INSERT INTO {TableName}({Fields}) VALUES({Values});";

//                return Execute(queryString);
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return 0;
//            }
//        }

//        /// <summary>
//        /// 执行删除操作
//        /// </summary>
//        /// <param name="TableName"></param>
//        /// <param name="WhereParameter"></param>
//        /// <returns></returns>
//        public static int ExecuteDelete(string TableName, Dictionary<string, string> WhereParameter)
//        {
//            var queryString = string.Empty;

//            try
//            {
//                var _WhereDetail = WhereParameter.Select(Item => $"{Item.Key} = '{Item.Value}'")
//                    .Aggregate(string.Empty, (current, _Where) => current + _Where + "And");

//                _WhereDetail = _WhereDetail.Substring(0, _WhereDetail.Length - 3);

//                var _Delete = $"DELETE FROM {TableName} WHERE ";

//                queryString = $"{_Delete}{_WhereDetail};";

//                return Execute(queryString);
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return 0;
//            }
//        }

//        /// <summary>
//        /// 执行更新操作
//        /// </summary>
//        /// <param name="TableName"></param>
//        /// <param name="WhereParameter"></param>
//        /// <param name="UpdateParameter"></param>
//        /// <returns></returns>
//        public static int ExecuteUpdate(string TableName, Dictionary<string, string> WhereParameter, Dictionary<string, string> UpdateParameter)
//        {
//            var queryString = string.Empty;

//            try
//            {
//                var _UpdateDetail = UpdateParameter.Select(Item => $"{Item.Key} = '{Item.Value}'")
//                    .Aggregate(string.Empty, (current, _Update) => current + _Update + ",");

//                _UpdateDetail = _UpdateDetail.TrimEnd(',');

//                var _WhereDetail = WhereParameter.Select(Item => $"{Item.Key} = '{Item.Value}'")
//                    .Aggregate(string.Empty, (current, _Where) => current + _Where + "And");

//                _WhereDetail = _WhereDetail.Substring(0, _WhereDetail.Length - 3);

//                queryString = $"UPDATE {TableName} SET {_UpdateDetail} WHERE {_WhereDetail};";

//                return Execute(queryString);
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return 0;
//            }
//        }

//        /// <summary>
//        /// 执行更新操作
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="TableName"></param>
//        /// <param name="AgoData"></param>
//        /// <param name="NewData"></param>
//        /// <returns></returns>
//        public static int ExecuteUpdate<T>(string TableName, T AgoData, T NewData)
//        {
//            try
//            {
//                string WhereFields = string.Empty,
//                       SetValues = string.Empty;

//                var Ago_pi = AgoData.GetType().GetProperties();

//                var New_pi = NewData.GetType().GetProperties();

//                foreach (var p in Ago_pi)
//                {
//                    string Field = $"{p.Name} = '{p.GetValue(AgoData)}'&";

//                    WhereFields += Field;
//                }

//                foreach (var p in New_pi)
//                {
//                    string Field = $"{p.Name} = '{p.GetValue(NewData)}',";

//                    SetValues += Field;
//                }


//                WhereFields = WhereFields.TrimEnd('&');
//                WhereFields = WhereFields.Replace("&", " AND ");

//                SetValues = SetValues.TrimEnd(',');

//                string queryString = $"UPDATE {TableName} SET {SetValues} WHERE {WhereFields};";
//                return Execute(queryString);
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 创建数据库表
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="TableName"></param>
//        /// <param name="Data"></param>
//        /// <returns></returns>
//        public static string CreatTable<T>(string TableName, T Data)
//        {
//            var queryString = string.Empty;

//            try
//            {
//                string Fields = string.Empty;

//                var pi = Data.GetType().GetProperties();

//                foreach (var p in pi)
//                {
//                    string Field = "" + p.Name + " varchar(50)";

//                    Fields = Fields + Field + ",";
//                }

//                Fields = Fields.TrimEnd(',');

//                queryString = $"CREATE TABLE {TableName}({Fields});";

//                return queryString;
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return string.Empty;
//            }
//        }

//        /// <summary>
//        /// 执行SQL查询
//        /// </summary>
//        /// <param name="queryString"></param>
//        /// <returns></returns>
//        public static int Execute(string queryString)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(SqlConn))
//                {
//                    var command = new SqlCommand(queryString, connection);
//                    command.Connection.Open();
//                    return command.ExecuteNonQuery();
//                }
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return 0;
//            }
//        }

//        /// <summary>
//        /// 将异常信息保存至文本文件
//        /// </summary>
//        /// <param name="Detail"></param>
//        /// <param name="reason"></param>
//        public static void SaveErrToText(string Detail, string reason)
//        {
//            const string FilePath = @"C:\WCSErrLog";
//            var FileName = DateTime.UtcNow.ToString("yyyyMMdd");

//            StreamWriter sw = null;
//            FileStream fs = null;

//            try
//            {
//                if (!Directory.Exists(FilePath))
//                {
//                    Directory.CreateDirectory(FilePath);
//                }

//                FileName = "WcsErrLog_" + FileName + ".txt";

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

//        /// <summary>
//        /// 执行删除操作
//        /// </summary>
//        /// <param name="TableName"></param>
//        /// <returns></returns>
//        public static int ClearData(string TableName)
//        {
//            var queryString = $"DELETE FROM {TableName}";

//            try
//            {
//                return Execute(queryString);
//            }
//            catch (Exception e)
//            {
//                SaveErrToText(queryString, e.Message);

//                return 0;
//            }
//        }
//    }
//}

using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace WCS_Helper
{
    /// <summary>
    /// wcs数据库连接（PostgreSQL 版本）
    /// </summary>
    public static class WcsSQLServer
    {
        private static readonly string SqlConn = ConfigurationHelper.GetConnectionString(); // 获取 PostgreSQL 连接字符串

        /// <summary>
        /// 执行查询，返回是否有结果
        /// </summary>
        public static bool SelectResult(string queryString)
        {
            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var adapter = new NpgsqlDataAdapter
                    {
                        SelectCommand = new NpgsqlCommand(queryString, connection)
                    };

                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    return dataTable.Rows.Count != 0;
                }
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return false;
            }
        }

        /// <summary>
        /// 执行存储过程（PostgreSQL 中使用函数或存储过程）
        /// </summary>
        public static void ExecuteProcedure(string sprocName)
        {
            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var comm = new NpgsqlCommand(sprocName, connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    connection.Open();
                    comm.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                SaveErrToText(sprocName, e.Message);
            }
        }

        /// <summary>
        /// 执行查询并返回 DataTable
        /// </summary>
        public static DataTable SelectContentToDataTable(string queryString)
        {
            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var adapter = new NpgsqlDataAdapter
                    {
                        SelectCommand = new NpgsqlCommand(queryString, connection)
                    };
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    return dataTable;
                }
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return null;
            }
        }

        /// <summary>
        /// 执行查询并返回泛型列表
        /// </summary>
        public static List<T> SelectContentToDataTable<T>(string queryString)
        {
            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var adapter = new NpgsqlDataAdapter
                    {
                        SelectCommand = new NpgsqlCommand(queryString, connection)
                    };
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        return ConvertToModel<T>(dataTable);
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return null;
            }
        }

        /// <summary>
        /// 将 DataTable 转换为泛型列表（保持不变）
        /// </summary>
        private static List<T> ConvertToModel<T>(DataTable dt)
        {
            var result = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                var columns = dr.Table.Columns;
                var t = Activator.CreateInstance<T>();
                var properties = t.GetType().GetProperties();

                foreach (var p in properties)
                {
                    string name = p.Name;
                    string lowerName = name.ToLower();

                    foreach (var c in columns)
                    {
                        string fieldName = c.ToString();
                        string lowerField = fieldName.ToLower();

                        if (lowerField != lowerName) continue;

                        p.SetValue(t, Convert.ChangeType(dr[fieldName], p.PropertyType), null);
                        break;
                    }
                }

                result.Add(t);
            }

            return result;
        }

        /// <summary>
        /// 根据表格名字查询表格信息（已移除 WITH(NOLOCK)）
        /// </summary>
        public static List<T> SelectContent<T>(string tableName)
        {
            var queryString = $"SELECT * FROM {tableName};";

            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var adapter = new NpgsqlDataAdapter
                    {
                        SelectCommand = new NpgsqlCommand(queryString, connection)
                    };
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    return dataTable.Rows.Count > 0 ? ConvertToModel<T>(dataTable) : new List<T>();
                }
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return new List<T>();
            }
        }

        /// <summary>
        /// 查询数据并返回泛型列表（直接使用 SQL）
        /// </summary>
        public static List<T> QueryContent<T>(string queryString)
        {
            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var adapter = new NpgsqlDataAdapter
                    {
                        SelectCommand = new NpgsqlCommand(queryString, connection)
                    };
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    return dataTable.Rows.Count > 0 ? ConvertToModel<T>(dataTable) : new List<T>();
                }
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return new List<T>();
            }
        }

        /// <summary>
        /// 执行插入操作（注意：存在 SQL 注入风险，建议改用参数化查询）
        /// </summary>
        public static int ExecuteInsert<T>(string tableName, T data)
        {
            var queryString = string.Empty;

            try
            {
                string fields = string.Empty, values = string.Empty;
                var properties = data.GetType().GetProperties();

                foreach (var p in properties)
                {
                    string field = p.Name;
                    object value = p.GetValue(data);
                    // 简单转义：将单引号替换为两个单引号（PostgreSQL 兼容）
                    string safeValue = value?.ToString().Replace("'", "''") ?? "NULL";
                    fields += field + ",";
                    values += $"'{safeValue}',";
                }

                fields = fields.TrimEnd(',');
                values = values.TrimEnd(',');

                queryString = $"INSERT INTO {tableName}({fields}) VALUES({values});";
                return Execute(queryString);
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return 0;
            }
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        public static int ExecuteDelete(string tableName, Dictionary<string, string> whereParameter)
        {
            var queryString = string.Empty;

            try
            {
                var whereClause = whereParameter.Select(item => $"{item.Key} = '{item.Value}'")
                                                 .Aggregate((current, next) => current + " AND " + next);
                queryString = $"DELETE FROM {tableName} WHERE {whereClause};";
                return Execute(queryString);
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return 0;
            }
        }

        /// <summary>
        /// 执行更新操作（使用两个字典）
        /// </summary>
        public static int ExecuteUpdate(string tableName, Dictionary<string, string> whereParameter, Dictionary<string, string> updateParameter)
        {
            var queryString = string.Empty;

            try
            {
                var setClause = updateParameter.Select(item => $"{item.Key} = '{item.Value}'")
                                                .Aggregate((current, next) => current + ", " + next);
                var whereClause = whereParameter.Select(item => $"{item.Key} = '{item.Value}'")
                                                .Aggregate((current, next) => current + " AND " + next);
                queryString = $"UPDATE {tableName} SET {setClause} WHERE {whereClause};";
                return Execute(queryString);
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return 0;
            }
        }

        /// <summary>
        /// 执行更新操作（使用两个实体对象）
        /// </summary>
        public static int ExecuteUpdate<T>(string tableName, T agoData, T newData)
        {
            try
            {
                string whereFields = string.Empty, setValues = string.Empty;
                var agoProps = agoData.GetType().GetProperties();
                var newProps = newData.GetType().GetProperties();

                foreach (var p in agoProps)
                {
                    object value = p.GetValue(agoData);
                    string safeValue = value?.ToString().Replace("'", "''") ?? "NULL";
                    whereFields += $"{p.Name} = '{safeValue}' AND ";
                }
                whereFields = whereFields.Substring(0, whereFields.Length - 5); // 移除最后的 " AND "

                foreach (var p in newProps)
                {
                    object value = p.GetValue(newData);
                    string safeValue = value?.ToString().Replace("'", "''") ?? "NULL";
                    setValues += $"{p.Name} = '{safeValue}', ";
                }
                setValues = setValues.TrimEnd(',', ' ');

                string queryString = $"UPDATE {tableName} SET {setValues} WHERE {whereFields};";
                return Execute(queryString);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 创建数据库表（简单实现，所有列均为 varchar(50)）
        /// </summary>
        public static string CreatTable<T>(string tableName, T data)
        {
            var queryString = string.Empty;

            try
            {
                string fields = string.Empty;
                var properties = data.GetType().GetProperties();

                foreach (var p in properties)
                {
                    fields += $"{p.Name} varchar(50),";
                }
                fields = fields.TrimEnd(',');

                queryString = $"CREATE TABLE {tableName} ({fields});";
                return queryString;
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// 执行 SQL 非查询语句
        /// </summary>
        public static int Execute(string queryString)
        {
            try
            {
                using (var connection = new NpgsqlConnection(SqlConn))
                {
                    var command = new NpgsqlCommand(queryString, connection);
                    command.Connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return 0;
            }
        }

        /// <summary>
        /// 将异常信息保存至文本文件（路径改为 C:\WCSErrLog）
        /// </summary>
        public static void SaveErrToText(string detail, string reason)
        {
            const string filePath = @"C:\WCSErrLog";
            var fileName = DateTime.UtcNow.ToString("yyyyMMdd");

            StreamWriter sw = null;
            FileStream fs = null;

            try
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                fileName = "WcsErrLog_" + fileName + ".txt";
                var fullPath = Path.Combine(filePath, fileName);

                if (!File.Exists(fullPath))
                {
                    fs = File.Create(fullPath);
                    fs.Close();
                    fs.Dispose();
                }

                sw = File.AppendText(fullPath);
                var content = $"DealMessage:{detail} \n Reason:{reason} \n Datetime:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} \n";
                sw.Write(content);
                sw.Close();
            }
            catch
            {
                sw?.Close();
                fs?.Close();
            }
        }

        /// <summary>
        /// 清空表数据
        /// </summary>
        public static int ClearData(string tableName)
        {
            var queryString = $"DELETE FROM {tableName}";
            try
            {
                return Execute(queryString);
            }
            catch (Exception e)
            {
                SaveErrToText(queryString, e.Message);
                return 0;
            }
        }
    }
}