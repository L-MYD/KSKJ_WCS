using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.PLCModel
{
    // 定义用于传送可视化的类
    public class ConveVisu
    {
        /// <summary>
        /// 数据唯一标识
        /// </summary>
        public string DataGuid { get; set; } = Guid.NewGuid().ToString("N");
        /// <summary>
        /// PLC IP地址
        /// </summary>
        public string PLCIP { get; set; }
        /// <summary>
        /// 索引编号
        /// </summary>
        public int IndexNum { get; set; }
        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNum { get; set; }
        /// <summary>
        /// 状态类型
        /// </summary>
        public int StateType { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// 托盘码
        /// </summary>
        public string Podid { get; set; }
    }
}
