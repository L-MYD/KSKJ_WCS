using WCS_Common.Enums;

namespace WCS_Helper.Database
{
    /// <summary>
    /// 数据库上下文工厂接口（参考 DTcms.Core 的 IDbContextFactory）。
    /// 屏蔽不同数据库的创建差异，对外统一返回 WcsDbContext。
    /// </summary>
    public interface IDbContextFactory
    {
        /// <summary>
        /// 创建一个数据库上下文实例（默认写库）。
        /// 返回的实例实现了 IDisposable，调用方应在 using 中使用或手动 Dispose。
        /// </summary>
        WcsDbContext CreateContext();

        /// <summary>
        /// nan_T 2026-09-09：按读写类型创建上下文（对齐 DTcms.Core 的 CreateContext(WriteRoRead)）。
        /// 当前 WCS 为单库模式，读/写返回同一连接；后续接入读写分离时，
        /// 只需在工厂实现中按 Write/Read 返回不同连接串，业务层无需改动。
        /// </summary>
        /// <param name="writeRoRead">操作类型：Write=写库，Read=读库/从库</param>
        WcsDbContext CreateContext(WriteRoRead writeRoRead);
    }
}
