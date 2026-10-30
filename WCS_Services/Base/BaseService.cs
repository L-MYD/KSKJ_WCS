using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WCS_Common.Enums;
using WCS_Common.Helper;
using WCS_Helper.Database;
using WCS_IServices;

namespace WCS_Services
{
    /// <summary>
    /// nan_T 2026-09-09：所有业务服务的泛型基类（参考 DTcms.Core.Services.BaseService）。
    /// 通过依赖注入的 <see cref="IDbContextFactory"/> 创建 <see cref="WcsDbContext"/>，
    /// 为子类提供统一的单表增删改查/分页能力，子类无需直接关心数据库类型与连接串。
    /// 业务服务继承本类并实现对应 XxxService 接口即可（如 LoginService : BaseService, ILoginService）。
    /// </summary>
    public class BaseService : IBaseService
    {
        /// <summary>nan_T 2026-09-09：当前操作使用的数据库上下文（每次调用泛型方法时由工厂创建）</summary>
        protected WcsDbContext? _context { get; set; }

        /// <summary>nan_T 2026-09-09：数据库上下文工厂，子类需要自定义复杂查询时可通过它自行创建上下文</summary>
        protected IDbContextFactory _contextFactory { get; private set; }

        /// <summary>
        /// 构造函数：依赖注入数据库上下文工厂
        /// </summary>
        /// <param name="contextFactory">数据库工厂（由 DI 容器自动注入）</param>
        public BaseService(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// 检查满足条件的记录是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class
        {
            _context = _contextFactory.CreateContext(writeAndRead);
            return await _context.Set<T>().AnyAsync(funcWhere);
        }

        /// <summary>
        /// 查询单条记录（带条件）
        /// </summary>
        public virtual async Task<T?> QueryAsync<T>(Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class
        {
            _context = _contextFactory.CreateContext(writeAndRead);
            return await _context.Set<T>().FirstOrDefaultAsync(funcWhere);
        }

        /// <summary>
        /// 查询符合条件的列表（可指定最大条数）
        /// </summary>
        public virtual async Task<IEnumerable<T>> QueryListAsync<T>(int top, Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class
        {
            _context = _contextFactory.CreateContext(writeAndRead); //连接数据库
            IQueryable<T> result = _context.Set<T>().Where(funcWhere); //条件筛选
            if (top > 0) result = result.Take(top); //top 大于 0 时限制条数，等于 0 返回全部
            return await result.ToListAsync();
        }

        /// <summary>
        /// 分页查询（带排序）
        /// </summary>
        public virtual async Task<PaginationList<T>> QueryPageAsync<T>(int pageSize, int pageIndex, Expression<Func<T, bool>> funcWhere,
            string orderBy, WriteRoRead writeAndRead = WriteRoRead.Read) where T : class
        {
            _context = _contextFactory.CreateContext(writeAndRead); //连接数据库
            IQueryable<T> result = _context.Set<T>().Where(funcWhere); //条件筛选
            result = ApplyOrderBy(result, orderBy); //动态排序
            return await PaginationList<T>.CreateAsync(pageIndex, pageSize, result);
        }

        /// <summary>
        /// 新增一条数据
        /// </summary>
        public virtual async Task<T> AddAsync<T>(T t, WriteRoRead writeAndRead = WriteRoRead.Write) where T : class
        {
            // nan_T 2026-09-09：增删改不允许指向只读库
            if (writeAndRead == WriteRoRead.Read)
            {
                throw new ResponseException("增删改操作不能指向只读库！");
            }
            _context = _contextFactory.CreateContext(writeAndRead);
            await _context.Set<T>().AddAsync(t);
            await SaveAsync();
            return t;
        }

        /// <summary>
        /// 修改一条数据
        /// </summary>
        public virtual async Task<bool> UpdateAsync<T>(T t, WriteRoRead writeAndRead = WriteRoRead.Write) where T : class
        {
            if (t == null)
            {
                throw new ResponseException("待更新数据不能为NULL！");
            }
            _context = _contextFactory.CreateContext(writeAndRead);
            _context.Set<T>().Attach(t); //将实体挂载到上下文
            _context.Entry(t).State = EntityState.Modified; //标记为已修改
            return await SaveAsync();
        }

        /// <summary>
        /// 根据条件删除数据
        /// </summary>
        public virtual async Task<bool> DeleteAsync<T>(Expression<Func<T, bool>> funcWhere, WriteRoRead writeAndRead = WriteRoRead.Write) where T : class
        {
            _context = _contextFactory.CreateContext(writeAndRead);
            var tList = await _context.Set<T>().Where(funcWhere).ToListAsync();
            if (tList == null || tList.Count == 0)
            {
                return false;
            }
            _context.Set<T>().RemoveRange(tList);
            return await SaveAsync();
        }

        /// <summary>
        /// 保存当前上下文变更（统一提交，保证事务）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            if (_context == null) return false;
            return (await _context.SaveChangesAsync() >= 0);
        }

        /// <summary>
        /// nan_T 2026-09-09：动态排序辅助方法。
        /// 支持排序字符串格式："字段名" 表示升序，"-字段名" 表示降序（如 "Id"、"-CreateTime"）；
        /// 不传排序字段时保持原顺序，由数据库返回默认顺序。
        /// </summary>
        private static IQueryable<T> ApplyOrderBy<T>(IQueryable<T> source, string? orderBy) where T : class
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return source; // nan_T 2026-09-09：未指定排序则不拼接 OrderBy
            }

            // nan_T 2026-09-09：取第一个排序字段解析（多字段排序可后续扩展逗号分隔）
            string first = orderBy.Split(',')[0].Trim();
            bool descending = first.StartsWith("-");
            string propName = descending ? first[1..] : first;

            PropertyInfo? prop = typeof(T).GetProperty(propName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                return source; // nan_T 2026-09-09：排序字段不存在时忽略排序，避免抛异常
            }

            // nan_T 2026-09-09：构造 x => x.PropName 的 Lambda 并调用 Queryable.OrderBy/OrderByDescending
            var parameter = Expression.Parameter(typeof(T), "x");
            var member = Expression.Property(parameter, prop);
            var lambda = Expression.Lambda(member, parameter);
            string methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), prop.PropertyType);

            return (IQueryable<T>)method.Invoke(null, new object[] { source, lambda })!;
        }
    }
}
