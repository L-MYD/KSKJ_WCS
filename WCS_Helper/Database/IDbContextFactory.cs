namespace WCS_Helper.Database
{
    /// <summary>
    /// 数据库上下文工厂接口（参考 DTcms.Core 的 IDbContextFactory）。
    /// 屏蔽不同数据库的创建差异，对外统一返回 WcsDbContext。
    /// </summary>
    public interface IDbContextFactory
    {
        /// <summary>
        /// 创建一个数据库上下文实例。
        /// 返回的实例实现了 IDisposable，调用方应在 using 中使用或手动 Dispose。
        /// </summary>
        WcsDbContext CreateContext();
    }
}
