using System.Linq.Expressions;
using WCS_Common.Enums;
using WCS_Common.Helper;

namespace WCS_IServices
{
    /// <summary>
    /// nan_T 2026-09-09：所有业务服务的泛型基接口（参考 DTcms.Core.IServices.IBaseService）。
    /// 把“单表增删改查、分页、存在性判断”等通用数据访问能力收敛到一处，
    /// 各业务服务接口继承本接口后即可直接使用，无需为每张表重复编写仓储代码。
    /// 泛型参数 T 为映射到数据库表的实体类（必须是引用类型）。
    /// </summary>
    public interface IBaseService
    {
        /// <summary>
        /// 检查满足条件的记录是否存在
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="funcWhere">查询条件表达式</param>
        /// <param name="writeAndRead">读库/写库选择，默认读库</param>
        Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class;

        /// <summary>
        /// 查询单条记录（带条件）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="funcWhere">查询条件表达式</param>
        /// <param name="writeAndRead">读库/写库选择，默认读库</param>
        /// <returns>匹配的实体；不存在返回 null</returns>
        Task<T?> QueryAsync<T>(Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class;

        /// <summary>
        /// 查询符合条件的列表（可指定最大条数）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="top">最多返回条数，0 表示不限制</param>
        /// <param name="funcWhere">查询条件表达式</param>
        /// <param name="writeAndRead">读库/写库选择，默认读库</param>
        Task<IEnumerable<T>> QueryListAsync<T>(int top, Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class;

        /// <summary>
        /// 分页查询（带排序）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pageSize">每页条数</param>
        /// <param name="pageIndex">当前页码（从 1 开始）</param>
        /// <param name="funcWhere">查询条件表达式</param>
        /// <param name="orderBy">排序字段（支持 “字段名” 升序、“-字段名” 降序）</param>
        /// <param name="writeAndRead">读库/写库选择，默认读库</param>
        Task<PaginationList<T>> QueryPageAsync<T>(int pageSize, int pageIndex, Expression<Func<T, bool>> funcWhere, string orderBy, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class;

        /// <summary>
        /// 新增一条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">待新增实体</param>
        /// <param name="writeAndRead">读库/写库选择，写操作默认走写库</param>
        /// <returns>保存后的实体（自增主键会被回填）</returns>
        Task<T> AddAsync<T>(T t, WriteRoRead writeAndRead = WriteRoRead.Write) where T : class;

        /// <summary>
        /// 修改一条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">待更新实体（按主键追踪）</param>
        /// <param name="writeAndRead">读库/写库选择，默认走写库</param>
        Task<bool> UpdateAsync<T>(T t, WriteRoRead writeAndRead = WriteRoRead.Write) where T : class;

        /// <summary>
        /// 根据条件删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="funcWhere">删除条件表达式</param>
        /// <param name="writeAndRead">读库/写库选择，默认走写库</param>
        Task<bool> DeleteAsync<T>(Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Write) where T : class;

        /// <summary>
        /// 保存当前上下文的变更（用于同一上下文内多次操作后统一提交，保证事务）
        /// </summary>
        Task<bool> SaveAsync();
    }
}
