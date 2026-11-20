using System.Collections.Generic;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// nan_T 2026-11-18：WMS 查询站点流向请求模型。
    /// StationCodes 为空时返回全部站点的流向。
    /// </summary>
    public class StationFlowQueryModel
    {
        /// <summary>
        /// 站点编码集合（WMS 站点名或 TES 站点编码均可）
        /// </summary>
        public List<string> StationCodes { get; set; }
    }

    /// <summary>
    /// nan_T 2026-11-18：站点流向明细。
    /// </summary>
    public class StationFlowModel
    {
        /// <summary>
        /// TES 站点编码
        /// </summary>
        public string StationCode { get; set; }

        /// <summary>
        /// WMS 站点名称
        /// </summary>
        public string WmsStationName { get; set; }

        /// <summary>
        /// TES 出库口站点名称
        /// </summary>
        public string TesStationName { get; set; }

        /// <summary>
        /// TES 入库口检测站点名称
        /// </summary>
        public string TesCheckName { get; set; }

        /// <summary>
        /// 站点流向：入库 / 出库 / 占用
        /// </summary>
        public string Flow { get; set; }
    }

    /// <summary>
    /// nan_T 2026-11-18：WMS 查询站点流向返回模型。
    /// </summary>
    public class StationFlowReturn
    {
        /// <summary>
        /// S 成功；F 业务失败；E 系统异常
        /// </summary>
        public string msgType { get; set; }

        /// <summary>
        /// 消息文本
        /// </summary>
        public string msgText { get; set; }

        /// <summary>
        /// 站点流向明细
        /// </summary>
        public List<StationFlowModel> data { get; set; }
    }
}
