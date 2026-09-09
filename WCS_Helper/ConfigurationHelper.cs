using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using WCS_Common.Enums;

namespace WCS_Helper
{
    /// <summary>
    /// 配置读取帮助类（参考 DTcms.Core 的 Appsettings）。
    /// 统一从 appsettings.json 读取“数据库类型 + 连接字符串”，供 DbContext 与工厂使用。
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>全局配置根对象</summary>
        private static IConfiguration _configuration;

        /// <summary>
        /// 静态构造：定位到程序运行目录并加载 appsettings.json，支持文件变更后热重载。
        /// </summary>
        static ConfigurationHelper()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        /// <summary>
        /// 获取当前数据库类型枚举，读取配置 ConnectionStrings:DBType。
        /// 配置缺失或无法识别时，默认返回 PostgreSQL，保证程序可启动。
        /// </summary>
        public static DBType GetDBType()
        {
            // 读取数据库类型原始字符串
            var raw = _configuration["ConnectionStrings:DBType"];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DBType.PostgreSQL;
            }

            // 兼容不同写法 / 别名（忽略大小写）
            switch (raw.Trim().ToLowerInvariant())
            {
                case "sqlserver":
                case "mssql":
                    return DBType.SqlServer;
                case "mysql":
                    return DBType.MySql;
                case "postgresql":
                case "postgres":
                case "npgsql":
                    return DBType.PostgreSQL;
                default:
                    // 兜底：直接按枚举名称解析，失败再回退 PostgreSQL
                    return Enum.TryParse<DBType>(raw, ignoreCase: true, out var type)
                        ? type
                        : DBType.PostgreSQL;
            }
        }

        /// <summary>
        /// 获取数据库连接字符串。
        /// 不传 name 时：优先取 WriteConnection（对齐 DTcms 的写库连接），为空则回退 DefaultConnection；
        /// 显式传入 name 时：读取 ConnectionStrings 下指定名称的连接串（向后兼容旧调用）。
        /// </summary>
        /// <param name="name">连接串名称，可为空</param>
        public static string GetConnectionString(string name = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return _configuration.GetConnectionString(name);
            }

            // 单库模式统一使用 WriteConnection
            var writeConnection = _configuration.GetConnectionString("WriteConnection");
            return string.IsNullOrWhiteSpace(writeConnection)
                ? _configuration.GetConnectionString("DefaultConnection")
                : writeConnection;
        }
    }
}
