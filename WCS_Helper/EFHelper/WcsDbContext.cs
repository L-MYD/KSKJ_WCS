using Microsoft.EntityFrameworkCore;
using WCS_Common.Enums;
using WCS_Helper;
using WCS_Models.LoginViewModel;
using WCS_Models.PLCModel;
using WCS_Models.SqlModel;
using WCS_Models.WCSModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Models.WMSModel;

/// <summary>
/// WCS 系统统一数据库上下文（参考 DTcms.Core 的 AppDbContext 结构）。
/// 通过 <see cref="DBType"/> 在 PostgreSQL / SQL Server / MySQL 三种数据库之间切换，
/// 上层的 TaskDbContext / UserDbContext / LogsDbContext 等仓储均基于本上下文访问数据。
/// </summary>
public class WcsDbContext : DbContext
{
    // ===== 业务实体对应的数据集（DbSet）=====
    public DbSet<TescorrespondsWms> TescorrespondsWms { get; set; }
    public DbSet<WCSAddPodModel> AddPod { get; set; }
    public DbSet<Aisles> Aisles { get; set; }
    public DbSet<StationCodeWithNum> StationCodeWithNum { get; set; }
    public DbSet<AgvPalletStation> AgvPalletStation { get; set; }
    public DbSet<ConveVisu> ConveVisu { get; set; }
    public DbSet<Stationstatus> Stationstatus { get; set; }
    public DbSet<LoginSqlModel> Users { get; set; }
    public DbSet<LogModel> Logs { get; set; }
    public DbSet<EmptyPodIDApply> EmptyPodIDApply { get; set; }

    /// <summary>当前使用的数据库类型（PostgreSQL / SqlServer / MySql），默认 SqlServer</summary>
    private DBType _dbType = DBType.SqlServer;

    /// <summary>当前生效的数据库连接字符串</summary>
    private string _connectionString = string.Empty;

    /// <summary>
    /// 构造函数1（依赖注入用）：当通过 services.AddDbContext 从外部传入 DbContextOptions 时使用。
    /// </summary>
    public WcsDbContext(DbContextOptions<WcsDbContext> options) : base(options) { }

    /// <summary>
    /// 构造函数2（工厂用，参考 DTcms.Core.AppDbContext）：显式指定数据库类型与连接串，
    /// 由 DbContextFactory.CreateContext 调用。
    /// </summary>
    /// <param name="dbType">数据库类型，传 null 时默认 PostgreSQL</param>
    /// <param name="connectionString">对应数据库的连接字符串</param>
    public WcsDbContext(DBType? dbType, string connectionString)
    {
        _dbType = dbType ?? DBType.PostgreSQL;
        _connectionString = connectionString ?? string.Empty;
    }

    /// <summary>
    /// 构造函数3（无参，供现有静态仓储直接 new WcsDbContext() 使用）：
    /// 自动从 appsettings.json 读取数据库类型与连接串，无需业务层关心当前是哪种数据库。
    /// </summary>
    public WcsDbContext()
    {
        _dbType = ConfigurationHelper.GetDBType();
        _connectionString = ConfigurationHelper.GetConnectionString();
    }

    /// <summary>
    /// 数据库提供程序配置：仅在未被外部 DbContextOptions 配置时生效。
    /// 依据 _dbType 在三种数据库之间切换（参考 DTcms.Core.AppDbContext 的 switch 写法）。
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            switch (_dbType)
            {
                // Microsoft SQL Server：使用 Microsoft.EntityFrameworkCore.SqlServer 提供程序
                case DBType.SqlServer:
                    optionsBuilder.UseSqlServer(_connectionString);
                    break;

                // MySQL：使用 Pomelo.EntityFrameworkCore.MySql 提供程序
                case DBType.MySql:
                    // 固定按 MySQL 8.0 版本生成 SQL；若希望启动时自动探测版本，可改为 ServerVersion.AutoDetect(_connectionString)
                    optionsBuilder.UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
                    break;

                // PostgreSQL（默认）：使用 Npgsql 提供程序
                case DBType.PostgreSQL:
                default:
                    optionsBuilder.UseNpgsql(_connectionString);
                    break;
            }

            // nan_T 2026-09-09：SQL 日志与敏感数据日志（会打印参数值，包含密码等）仅在开发环境开启，
            // 生产环境一律关闭，避免敏感信息泄露到控制台/日志采集系统。
            if (string.Equals(System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", System.StringComparison.OrdinalIgnoreCase))
            {
                optionsBuilder.LogTo(Console.WriteLine)
                  .EnableSensitiveDataLogging();
            }
        }
    }

    /// <summary>
    /// 模型创建：配置实体与数据表的映射（表名、主键、列名等），与具体数据库无关。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置表名和主键（如果与类名/属性名不一致）
        modelBuilder.Entity<TescorrespondsWms>(entity =>
        {
            // 表名映射（小写蛇形）
            entity.ToTable("tescorresponds_wms");

            // 主键配置（自增）
            entity.HasKey(e => e.id);
            entity.Property(e => e.id)
                  .UseIdentityColumn();  // 或 .ValueGeneratedOnAdd()

            // 列名映射
            entity.Property(e => e.WcsId).HasColumnName("wcs_id");
            entity.Property(e => e.TesTaskID).HasColumnName("tes_task_id");
            entity.Property(e => e.WmsTaskID).HasColumnName("wms_task_id");
            entity.Property(e => e.WmsTaskType).HasColumnName("wms_task_type");
            entity.Property(e => e.CurrentPosition).HasColumnName("current_position");
            entity.Property(e => e.Statu).HasColumnName("statu");
            entity.Property(e => e.CreationTime).HasColumnName("creation_time");
            entity.Property(e => e.CompletionTime).HasColumnName("completion_time");
            entity.Property(e => e.TargetLocation).HasColumnName("target_location");
            entity.Property(e => e.Podid).HasColumnName("pod_id");
        });

        modelBuilder.Entity<WCSAddPodModel>(entity =>
            {
                entity.ToTable("AddPod");
                entity.HasKey(e => e.PodID); // 假设 PodID 是主键
            });

        modelBuilder.Entity<Aisles>(entity =>
        {
            entity.ToTable("Aisles");
            entity.HasKey(e => e.Id); // 假设 Id 是主键
        });

        modelBuilder.Entity<StationCodeWithNum>(entity =>
        {
            entity.ToTable("StationCodeWithNum");
            entity.HasKey(e => e.code); // 假设 code 是主键
        });

        modelBuilder.Entity<AgvPalletStation>(entity =>
        {
            entity.ToTable("AgvPalletStation");
            entity.HasKey(e => e.Id); // 假设 id 是主键
        });

        modelBuilder.Entity<ConveVisu>(entity =>
        {
            entity.ToTable("ConveVisu");
            entity.HasKey(e => e.DataGuid); // 假设 DataGuid 是主键
        });

        modelBuilder.Entity<Stationstatus>(entity =>
        {
            entity.ToTable("stationstatus");
            entity.HasKey(e => e.Id);

            // 关键：将 Id 属性映射到数据库的 id 列（小写）
            entity.Property(e => e.Id)
                  .HasColumnName("id")  // ← 添加这一行
                  .UseIdentityColumn();

            entity.Property(e => e.StationCode).HasColumnName("stationcode");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.WmsStationName).HasColumnName("wmsstationname");
            entity.Property(e => e.TesCheckName).HasColumnName("tescheckname");
        });

        modelBuilder.Entity<LogModel>(entity =>
        {
            entity.ToTable("logs"); // 指定表名

            // 设置主键（假设 Id 属性存在，如果不存在请根据实际字段修改）
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<LoginSqlModel>(entity =>
        {
            entity.ToTable("Users"); // 指定表名
            entity.HasKey(e => e.Id); // 主键

            // 如果数据库列名与属性名不同，需要映射，例如：
            // entity.Property(e => e.UserName).HasColumnName("username");
            // entity.Property(e => e.Password).HasColumnName("password");
            // ... 根据需要配置
        });

        modelBuilder.Entity<ApiServer>(entity =>
        {
            entity.ToTable("ApiServer"); // 指定表名
            entity.HasKey(e => e.Id); // 主键

            // 如果数据库列名与属性名不同，需要映射，例如：
            // entity.Property(e => e.UserName).HasColumnName("username");
            // entity.Property(e => e.Password).HasColumnName("password");
            // ... 根据需要配置
        });

        modelBuilder.Entity<EmptyPodIDApply>(entity =>
        {
            entity.ToTable("EmptyPodIDApply");
            entity.HasKey(e => e.PodID); // 假设 id 是主键
        });
    }
}
