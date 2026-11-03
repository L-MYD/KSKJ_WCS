using System;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WCS 托盘入库申请 WMS：WCS→WMS 请求模型（POST /api/WMS/PalnoApply）
    /// nan_T 2026-10-30：按《WMS-WCS对接协议》定义字段
    /// </summary>
    public class PalnoApplyModel
    {
        /// <summary>
        /// WCS 流水号
        /// </summary>
        public string WcsId { get; set; }

        /// <summary>
        /// 申请点（WMS 站点名称）
        /// </summary>
        public string ApplyLocation { get; set; }

        /// <summary>
        /// 容器号
        /// </summary>
        public string Palno { get; set; }

        /// <summary>
        /// 容器高度 H：高 L：矮 M：中
        /// </summary>
        public string Height { get; set; }
    }

    /// <summary>
    /// WCS 托盘入库申请 WMS：WCS→WMS 返回模型
    /// </summary>
    public class PalnoApplyReturn
    {
        /// <summary>
        /// 消息类型 S:成功 E:程序异常 F:失败
        /// </summary>
        public string msgType { get; set; }

        /// <summary>
        /// 消息文本
        /// </summary>
        public string msgText { get; set; }

        /// <summary>
        /// WCS 流水号
        /// </summary>
        public string WcsId { get; set; }

        /// <summary>
        /// WMS 流水号
        /// </summary>
        public string WmsId { get; set; }

        /// <summary>
        /// 回执通道（通道号/单元货位）
        /// </summary>
        public string ToLocation { get; set; }
    }
}
