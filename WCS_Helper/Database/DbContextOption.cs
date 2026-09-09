using WCS_Common.Enums;

namespace WCS_Helper.Database
{
    /// <summary>
    /// 数据库上下文配置选项，对应 appsettings.json 中的 ConnectionStrings 节点。
    /// 参考 DTcms.Core.DBFactory.Database.DbContextOption 的结构设计。
    /// </summary>
    public class DbContextOption
    {
        /// <summary>数据库类型：PostgreSQL / SqlServer / MySql，默认 PostgreSQL</summary>
        public DBType DBType { get; set; } = DBType.PostgreSQL;

        /// <summary>写库连接字符串；在单库模式下它就是唯一使用的连接字符串</summary>
        public string WriteConnection { get; set; } = string.Empty;

        /// <summary>
        /// 读库连接字符串集合，用于读写分离 / 一主多从时在多个只读副本间负载。
        /// 当前项目为单库模式暂未启用，保留该字段以对齐参考结构、便于后续扩展。
        /// </summary>
        public List<string>? ReadConnectionList { get; set; }
    }
}
