using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    /// <summary>
    /// 站点映射类
    /// </summary>
    public class StationNameModel
    {
        /// <summary>
        /// WMS站点名称
        /// </summary>
        public string WmsStationName { get; set; }

        /// <summary>
        /// TES出库口站点名称
        /// </summary>
        public string TesStationName { get; set; }

        /// <summary>
        /// TES入库口检测站点名称
        /// </summary>
        public string TesCheckName {  get; set; }
    }
}
