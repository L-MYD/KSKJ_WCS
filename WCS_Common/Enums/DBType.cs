namespace WCS_Common.Enums
{
    /// <summary>
    /// 系统支持的数据库类型枚举（参考 DTcms.Core 的多数据库设计）。
    /// 由配置文件 ConnectionStrings:DBType 指定当前使用的数据库，
    /// WcsDbContext.OnConfiguring 会依据此枚举切换对应的 EF Core 提供程序。
    /// </summary>
    public enum DBType
    {
        /// <summary>PostgreSQL 数据库，使用 Npgsql 提供程序（系统当前默认）</summary>
        PostgreSQL = 0,

        /// <summary>Microsoft SQL Server 2012 及以上，使用 Microsoft.EntityFrameworkCore.SqlServer</summary>
        SqlServer = 1,

        /// <summary>MySQL 数据库，使用 Pomelo.EntityFrameworkCore.MySql</summary>
        MySql = 2
    }
}
