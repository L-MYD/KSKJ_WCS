using System;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// LED 显示 WCS→WMS 请求模型（POST /WMS/Bqs/LED）
    /// nan_T 2026-10-30：按《WMS-WCS对接协议》定义字段
    /// </summary>
    public class LEDModel
    {
        /// <summary>
        /// WCS 流水号
        /// </summary>
        public string WcsId { get; set; }

        /// <summary>
        /// 托盘号
        /// </summary>
        public string Palno { get; set; }

        /// <summary>
        /// 口地址
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// 操作时间，格式 yyyy-MM-dd HH:mm:ss
        /// </summary>
        public string Tkdat { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 状态 Open：打开 Close：熄灭
        /// </summary>
        public string Statu { get; set; }
    }

    /// <summary>
    /// 设备状态上报 / LED 显示的通用返回模型（msgType/msgText/WcsId）
    /// </summary>
    public class WmsSimpleReturn
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
    }
}
