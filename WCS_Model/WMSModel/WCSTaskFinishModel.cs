using System;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// 任务完成接口 WCS→WMS 请求模型（POST /api/WMS/WCSTaskFinish）
    /// nan_T 2026-10-30：按《WMS-WCS对接协议》定义字段
    /// </summary>
    public class WCSTaskFinishModel
    {
        /// <summary>
        /// WMS 的流水号（WMS 任务号）
        /// </summary>
        public string WmsId { get; set; }

        /// <summary>
        /// 托盘号
        /// </summary>
        public string Palno { get; set; }

        /// <summary>
        /// 操作时间，格式 yyyy-MM-dd HH:mm:ss
        /// </summary>
        public string Tkdat { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 状态：IS 入库开始执行 / IF 通道请求深度 / PF 托盘入库完毕 /
        /// SO 穿梭车出开始执行 / FO 输送机出库完成 / C 入库双重 / K 空取货 / FJ 到达拣选点
        /// </summary>
        public string Statu { get; set; }

        /// <summary>
        /// WCS 申请时生成的唯一流水号
        /// </summary>
        public string WcsId { get; set; }
    }

    /// <summary>
    /// 任务完成接口 WCS→WMS 返回模型
    /// </summary>
    public class WCSTaskFinishReturn
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
        /// WMS 货位地址，只有 Statu=IF 时给具体货位，其余为空
        /// </summary>
        public string Addre { get; set; }
    }
}
