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

        /// <summary>上报任务完成状态到 WMS（/api/WMS/WCSTaskFinish）</summary>
        Task<WCSTaskFinishReturn> GetWmsreceiveTask(WCSTaskFinishModel model);

        /// <summary>托盘入库申请（/api/WMS/PalnoApply）</summary>
        Task<PalnoApplyReturn> GetPalnoApply(PalnoApplyModel model);

        /// <summary>设备状态上报（/api/WMS/DeviceStatu）</summary>
        Task<WmsSimpleReturn> PostDeviceStatu(DeviceStatuModel model);

        /// <summary>LED 显示（/WMS/Bqs/LED）</summary>
        Task<WmsSimpleReturn> PostLed(LEDModel model);

        /// <summary>处理工位消息（60号报文）</summary>
        Task GetStationMess(MessageType60 model);
    }
}
