namespace WCS_Common.Enums
{
    /// <summary>
    /// nan_T 2026-09-09：数据库操作类型枚举（参考 DTcms.Core.Common.Emum.WriteRoRead）。
    /// 用于 DbContextFactory 创建上下文时区分“写库/读库”，
    /// 当前 WCS 为单库模式，读写指向同一连接；保留该枚举是为了后续接入读写分离时
    /// 业务层代码无需改动，只需在工厂中切换连接字符串。
    /// </summary>
    public enum WriteRoRead
    {
        /// <summary>写操作（指向主库）</summary>
        Write = 0,

        /// <summary>读操作（指向只读副本/从库）</summary>
        Read = 1
    }
}
