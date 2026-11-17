namespace WCS_Models.WMSModel
{
    /// <summary>
    /// nan_T 2026-11-16：WMS 站点释放请求模型（应急出库特殊流程）。
    /// 流程：WMS 将应急出库任务下发到拣选口的应急出库口 → 人工搬走货物 →
    ///       WMS 调用 WCS 站点释放接口（api/WCS/StationRelease）→ WCS 调用 TES 站点释放接口。
    /// </summary>
    public class StationReleaseModel
    {
        /// <summary>
        /// WMS 流水号（用于日志追踪与幂等）
        /// </summary>
        public string WmsId { get; set; }

        /// <summary>
        /// WMS 站点名称（拣选口的应急出库口）
        /// </summary>
        public string StationCode { get; set; }

        /// <summary>
        /// 释放原因（可选，仅用于日志记录）
        /// </summary>
        public string Reason { get; set; }
    }
}
