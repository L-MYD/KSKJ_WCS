using WCS_Models.WMSModel;

namespace WCS_IServices
{
    /// <summary>
    /// 对接 WMS（仓储管理系统）的应用服务接口。
    /// 由 WCS_Services 层的 ToWmsApiService 实现。
    /// </summary>
    public interface IToWmsApiService
    {
        /// <summary>根据 WMS 移动任务模型创建新的移动任务，返回 WCS 处理结果</summary>
        WcsReturn newMoveTask(WMSTaskDisModel model);

        /// <summary>
        /// nan_T 2026-11-16：WMS 站点释放（应急出库特殊流程）。
        /// WCS 将 WMS 站点名映射为 TES 站点编码，调用 TES 站点释放接口，并清除本地站点占用状态。
        /// </summary>
        WcsReturn releaseStation(StationReleaseModel model);
    }
}
