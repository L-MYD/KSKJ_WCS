using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// 设备任务状态上报 WMS：WCS->WMS  模型
    /// </summary>
    public class DeviceStatuModel
    {
        /// <summary>
        /// 设备状态
        /// </summary>
        public string Deviceno { get; set; }

        /// <summary>
        /// 状态 @：故障，B：脱机，A：联机，W：工作，C：待机
        /// </summary>
        public string Statu { get; set; }

        /// <summary>
        /// 异常原因
        /// </summary>
        public string ErrorReason { get; set; }

        /// <summary>
        /// WCS 流水号 
        /// </summary>
        public string WcsId { get; set; }
    }

}
