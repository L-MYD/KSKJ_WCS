using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class Stationstatus
    {
        /// <summary>
        /// // 新增主键
        /// </summary>
        public int Id { get; set; }          

        /// <summary>
        /// 站点名称
        /// </summary>
        public string StationCode {  get; set; }

        /// <summary>
        /// 站点流向
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// WMS站点名称
        /// </summary>
        public string WmsStationName { get; set; }

        /// <summary>
        /// 入库检查点
        /// </summary>
        public string TesCheckName { get; set; }
    }
}
