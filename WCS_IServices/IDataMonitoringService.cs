using WCS_Models.LoginViewModel;
using WCS_Models.SqlModel;

namespace WCS_IServices
{
    /// <summary>
    /// 数据监控服务接口：负责任务列表/历史任务的分页与条件查询。
    /// 由 WCS_Services 层的 DataMonitoringService 实现。
    /// nan_T 2026-09-09：按 DTcms.Core 分层约定继承 <see cref="IBaseService"/>，
    /// 除下方业务方法外，同时拥有泛型单表增删改查/分页能力。
    /// </summary>
    public interface IDataMonitoringService : IBaseService
    {
        /// <summary>
        /// 按查询条件分页获取当前任务列表（默认仅查询当天任务）。
        /// </summary>
        /// <param name="taskQueryDto">任务查询条件（页码、页大小、日期范围、状态、关键词等）</param>
        /// <returns>分页结果：当前页数据 + 总记录数 + 分页信息</returns>
        Task<QueryModel<TescorrespondsWms>> GetTasklistAll(TaskQueryDto taskQueryDto);

        /// <summary>
        /// 按查询条件分页获取历史任务列表（默认查询最近 7 天任务）。
        /// </summary>
        /// <param name="taskHistoryQueryParams">历史任务查询条件（页码、页大小、日期范围、状态、关键词等）</param>
        /// <returns>分页结果：当前页数据 + 总记录数 + 分页信息</returns>
        Task<QueryModel<TescorrespondsWms>> GetHistoryTasklistAll(TaskHistoryQueryParams taskHistoryQueryParams);
    }
}
