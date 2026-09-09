using System.Threading.Tasks;
using WCS_Models;
using WCS_Models.TESModel;
using WCS_Models.WMSModel;

namespace WCS_IServices
{
    /// <summary>
    /// 对接 TES（任务执行系统）的应用服务接口（实例方法部分）。
    /// 由 WCS_Services 层的 ToTesApiService 实现；该类中的静态辅助方法不纳入接口。
    /// </summary>
    public interface IToTesApiService
    {
        /// <summary>接收并处理 TES 下发的移动任务</summary>
        Task receiveTask(ToWcsPublicParameters model, TaskInfo content);

        /// <summary>查询 TES 任务详情</summary>
        TaskInfo getTaskDetail(string TesTskID);

        /// <summary>接收并处理托盘类任务</summary>
        Task receiveTaskPallent(ToWcsPublicParameters model, InspectionContent content);

        /// <summary>更新工位操作（61号报文）</summary>
        Task UpdateStationOperation(MessageType61 content);

        /// <summary>设置工位状态</summary>
        StationReturn setStationStatus(TessetStationStatus model);

        /// <summary>处理 WMS 回传的任务接收结果</summary>
        Task<ReturnWcs> GetWmsreceiveTask(UpdateTaskRequest model);

        /// <summary>获取空托盘申请</summary>
        Task<ReturnWcs> GetPalnoApply(UpdateTaskRequest model);

        /// <summary>提交空托盘申请</summary>
        Task<ReturnWcs> PostPalnoApply(UpdateEmptyPallet model);

        /// <summary>提交工位释放请求</summary>
        Task<ReleaseStationsHttpResqon> PostPalnoRelease(ReleaseStationsHttp model);

        /// <summary>处理工位消息（60号报文）</summary>
        Task GetStationMess(MessageType60 model);
    }
}
