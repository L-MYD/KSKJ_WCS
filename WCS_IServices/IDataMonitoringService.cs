using System.Threading.Tasks;
using WCS_Models.LoginViewModel;
using WCS_Models.SqlModel;

namespace WCS_IServices
{
    /// <summary>
    /// 数据监控服务接口：负责任务列表/历史任务的分页与条件查询。
    /// 由 WCS_Services 层的 DataMonitoringService 实现。
    /// </summary>
    public interface IDataMonitoringService
    {
        /// <summary>按查询条件分页获取当前任务列表</summary>
        Task<QueryModel<TescorrespondsWms>> GetTasklistAll(TaskQueryDto taskQueryDto);

        /// <summary>按查询条件分页获取历史任务列表</summary>
        Task<QueryModel<TescorrespondsWms>> GetHistoryTasklistAll(TaskHistoryQueryParams taskHistoryQueryParams);
    }
}
