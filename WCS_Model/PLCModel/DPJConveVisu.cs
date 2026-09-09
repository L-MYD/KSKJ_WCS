using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.PLCModel
{
    public class DPJConveVisu
    {
        /// <summary>
        /// 工位ID
        /// </summary>
        public string DeviceID { get; set; }
        /// <summary>
        /// 运行状态:00无异常
        /// </summary>
        public string DeviceStatus { get; set; }
        /// <summary>
        /// 托盘数量
        /// </summary>
        public string FoldNum { get; set; }
        /// <summary>
        /// 设备模式:01自动模式。02手动模式
        /// </summary>
        public string Mode { get; set; }
        /// <summary>
        /// 处理动作:00托盘可以进入。托盘不可进入
        /// </summary>
        public string FoldReady { get; set; }
        /// <summary>
        /// 容器号
        /// </summary>
        public string Podid { get; set; }
    }
}
