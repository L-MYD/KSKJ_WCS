using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.PLCModel
{
    // 定义用于发送旷视PLC传送消息的类
    public class ConveSendMess
    {
        /// <summary>
        /// 数据唯一标识
        /// </summary>
        public string DataGuid { get; set; } = Guid.NewGuid().ToString("N");
        /// <summary>
        /// PLC IP地址 一二楼输送线ip：172.18.18.20  自动入库口ip：10.101.86.53  手动出入库口ip：10.101.86.58
        /// </summary>
        public string PLCIP { get; set; } = "172.18.18.20";

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessType { get; set; } = "00";

        ///// <summary>
        ///// 
        ///// </summary>
        //public string ReceiveDevice { get; set; } = "0000";
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
        public string ToLocation { get; set; } = "0000";
        /// <summary>
        /// 托盘高度（剁型号也使用）StUnitType
        /// </summary>
        public string StUnitHeight { get; set; } = "0000";
        /// <summary>
        /// 托盘重量  StUnitSize
        /// </summary>
        public string StUnitWeight { get; set; } = "000000";
        /// <summary>
        /// 错误代码  DigCode
        /// </summary>
        public string ReasonCode { get; set; } = "00000000";
        /// <summary>
        /// 堆垛类型
        /// </summary>
        public string CanWrite { get; set; } = "01";
    }
}
