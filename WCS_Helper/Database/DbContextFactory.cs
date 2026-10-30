using WCS_Common.Enums;

namespace WCS_Helper.Database
{
    /// <summary>
    /// 数据库上下文工厂实现（参考 DTcms.Core 的 DbContextFactory）。
    /// 构造时从 appsettings.json 读取“数据库类型 + 连接字符串”，
    /// 每次 CreateContext 据此创建对应数据库的 <see cref="WcsDbContext"/>；
    /// 具体使用哪个 EF Core 提供程序，在 WcsDbContext.OnConfiguring 中按类型切换。
    /// </summary>
    public class DbContextFactory : IDbContextFactory
    {
        /// <summary>当前数据库类型（PostgreSQL / SqlServer / MySql）</summary>
        private readonly DBType _dbType;

        /// <summary>当前生效的数据库连接字符串</summary>
        private readonly string _connectionString;

        /// <summary>
        /// 构造函数：通过 <see cref="ConfigurationHelper"/> 读取配置文件中的数据库配置。
        /// </summary>
        public DbContextFactory()
        {
            _dbType = ConfigurationHelper.GetDBType();
            _connectionString = ConfigurationHelper.GetConnectionString();
        }

        /// <summary>
        /// 创建数据库上下文实例（默认写库）。
        /// </summary>
        /// <returns>已携带数据库类型与连接串的 WcsDbContext 实例</returns>
        public WcsDbContext CreateContext()
        {
            return new WcsDbContext(_dbType, _connectionString);
        }

        /// <summary>
        /// nan_T 2026-09-09：按读写类型创建上下文（对齐 DTcms.Core 的工厂方法）。
        /// 当前为单库模式：Write/Read 均使用同一连接串；
        /// 后续接入读写分离时，在此按 writeRoRead 选择 WriteConnection / ReadConnectionList 即可，
        /// BaseService 及各业务服务的调用代码无需任何改动。
        /// </summary>
        /// <param name="writeRoRead">操作类型：Write=写库，Read=读库/从库</param>
        /// <returns>对应数据库连接的 WcsDbContext 实例</returns>
        public WcsDbContext CreateContext(WriteRoRead writeRoRead)
        {
            // nan_T 2026-09-09：单库模式下读写同库；未来读库扩展点：
            // if (writeRoRead == WriteRoRead.Read && _readConnectionList != null) { ...轮询/随机选从库... }
            return new WcsDbContext(_dbType, _connectionString);
        }
    }
}
