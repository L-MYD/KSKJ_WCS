using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using WCS_Common.Enums;

namespace WCS_Helper
{
    /// <summary>
    /// nan_T 2026-09-09：EF Core 迁移设计时工厂（IDesignTimeDbContextFactory）。
    /// 执行 `dotnet ef migrations` / `dotnet ef database update` 时，EF 工具优先调用本工厂
    /// 创建 <see cref="WcsDbContext"/>，无需在 DI 容器注册 DbContext，也不启动整个 Web 宿主。
    /// 工厂从启动项目（KSKJ_WCS）目录的 appsettings.json 读取“数据库类型 + 连接串”，
    /// 保证迁移生成/应用时使用的数据库与运行时一致。
    /// </summary>
    public class WcsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<WcsDbContext>
    {
        /// <summary>
        /// 创建设计时用的 DbContext。
        /// </summary>
        /// <param name="args">dotnet ef 传入的命令行参数（可用于覆盖连接串等，本实现暂不处理）</param>
        /// <returns>已按 appsettings 配置好提供程序的 WcsDbContext</returns>
        public WcsDbContext CreateDbContext(string[] args)
        {
            // nan_T 2026-09-09：定位 appsettings.json——
            // 1) 优先用当前工作目录（dotnet ef 以启动项目为内容根时即为 KSKJ_WCS 目录）；
            // 2) 找不到则向上递归查找，兼容从其他目录执行命令的情况。
            string basePath = Directory.GetCurrentDirectory();
            string settingsPath = FindAppSettings(basePath);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(settingsPath)!)
                .AddJsonFile(Path.GetFileName(settingsPath), optional: false, reloadOnChange: false)
                .Build();

            DBType dbType = ParseDbType(configuration["ConnectionStrings:DBType"]);

            // nan_T 2026-09-09：与 ConfigurationHelper 口径一致，按数据库类型取专用连接串，
            // 缺失时回退 WriteConnection / DefaultConnection
            string providerKey = dbType switch
            {
                DBType.SqlServer => "SqlServerConnection",
                DBType.MySql => "MySqlConnection",
                DBType.PostgreSQL => "PostgreSqlConnection",
                _ => "PostgreSqlConnection"
            };

            string connectionString = configuration.GetConnectionString(providerKey);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration.GetConnectionString("WriteConnection");
            }
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            return new WcsDbContext(dbType, connectionString);
        }

        /// <summary>
        /// nan_T 2026-09-09：从指定目录开始向上递归查找 appsettings.json（取最近的一个）。
        /// </summary>
        private static string FindAppSettings(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                var file = dir.GetFiles("appsettings.json").FirstOrDefault();
                if (file != null)
                {
                    return file.FullName;
                }
                dir = dir.Parent;
            }

            // 兜底：直接返回当前目录下的约定路径（交给配置加载报清晰错误）
            return Path.Combine(startDir, "appsettings.json");
        }

        /// <summary>
        /// nan_T 2026-09-09：解析数据库类型字符串为枚举（与 ConfigurationHelper 口径一致，默认 MySql）。
        /// </summary>
        private static DBType ParseDbType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DBType.MySql;
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "sqlserver" or "mssql" => DBType.SqlServer,
                "mysql" => DBType.MySql,
                _ => DBType.MySql
            };
        }
    }
}
