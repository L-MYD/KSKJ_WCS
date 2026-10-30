namespace WCS_Common.Helper
{
    /// <summary>
    /// nan_T 2026-09-09：分页结果包装类（参考 DTcms.Core.Common.Helper.PaginationList）。
    /// 统一封装分页查询的返回结构：当前页数据 + 分页元信息，
    /// 便于前端分页组件直接消费。
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class PaginationList<T>
    {
        /// <summary>当前页的数据集合</summary>
        public IReadOnlyList<T> Items { get; private set; } = new List<T>();

        /// <summary>当前页码（从 1 开始）</summary>
        public int PageIndex { get; private set; }

        /// <summary>每页条数</summary>
        public int PageSize { get; private set; }

        /// <summary>符合条件的总记录数</summary>
        public int TotalCount { get; private set; }

        /// <summary>总页数</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        /// <summary>是否还有上一页</summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>是否还有下一页</summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// nan_T 2026-09-09：私有构造，统一通过 CreateAsync 工厂创建，
        /// 保证分页元信息与数据一致。
        /// </summary>
        private PaginationList(IReadOnlyList<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        /// <summary>
        /// nan_T 2026-09-09：根据 IQueryable 分页查询并包装结果。
        /// 调用方应在传入前完成 Where 条件过滤与排序。
        /// </summary>
        /// <param name="pageIndex">页码（从 1 开始，小于 1 自动修正为 1）</param>
        /// <param name="pageSize">每页条数（小于 1 自动修正为 10）</param>
        /// <param name="source">已完成过滤/排序的查询源</param>
        public static async Task<PaginationList<T>> CreateAsync(int pageIndex, int pageSize, IQueryable<T> source)
        {
            // nan_T 2026-09-09：页码与每页条数兜底，防止非法参数导致异常
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;

            int count = await Task.Run(() => source.Count());
            var items = await Task.Run(() => source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList());

            return new PaginationList<T>(items, count, pageIndex, pageSize);
        }
    }
}
