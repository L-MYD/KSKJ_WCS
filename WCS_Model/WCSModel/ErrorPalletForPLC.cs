using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class ErrorPalletForPLC
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageType { get; set; } = "00";
        /// <summary>
        /// 托盘码
        /// </summary>
        public string StUintID { get; set; } = "******************************";
        /// <summary>
        /// 起始位置
        /// </summary>
        public string FromLocation { get; set; } = "0000";
        /// <summary>
        /// 线路编号
        /// </summary>
        public string ToLacation { get; set; } = "0000";
        /// <summary>
        /// 托盘高度（剁型号也使用）
        /// </summary>
        public string StUnitHeight { get; set; } = "0000";
        /// <summary>
        /// 托盘重量
        /// </summary>
        public string StUnitWeight { get; set; } = "000000";
        /// <summary>
        /// 错误代码
        /// </summary>
        public string ReasonCode { get; set; } = "00000000";
        /// <summary>
        /// 堆垛类型
        /// </summary>
        public string CanWrite { get; set; } = "10";
    }
}
