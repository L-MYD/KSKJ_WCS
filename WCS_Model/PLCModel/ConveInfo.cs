using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.PLCModel
{
    public class ConveInfo
    {
        /// <summary>
        /// 数据唯一标识符
        /// </summary>
        public string DataGuid { get; set; } = Guid.NewGuid().ToString("N");
        /// <summary>
        /// PLC IP 地址
        /// </summary>
        public string PLCIP { get; set; } = "172.18.18.20";
        /// <summary>
        /// PLC 符号
        /// </summary>
        public string PLCSymbol { get; set; } = "";
        /// <summary>
        /// DB4 编号
        /// </summary>
        public int DB4Num { get; set; }
        /// <summary>
        /// DB5 编号
        /// </summary>
        public int DB5Num { get; set; }
        /// <summary>
        /// DB6 编号
        /// </summary>
        public int DB6Num { get; set; }
        /// <summary>
        /// DB4 长度
        /// </summary>
        public int DB4Len { get; set; }
        /// <summary>
        /// DB5 长度
        /// </summary>
        public int DB5Len { get; set; }
        /// <summary>
        /// DB6 长度
        /// </summary>
        public int DB6Len { get; set; }
        /// <summary>
        /// 机架号
        /// </summary>
        public int Rack { get; set; }
        /// <summary>
        /// 槽号
        /// </summary>
        public int Slot { get; set; }
        /// <summary>
        /// 接收状态，默认为 0
        /// </summary>
        public int RecvState { get; set; } = 0;
        /// <summary>
        /// 发送状态，默认为 0
        /// </summary>
        public int SendState { get; set; } = 0;
    }
}
