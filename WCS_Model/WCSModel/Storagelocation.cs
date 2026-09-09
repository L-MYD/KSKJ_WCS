using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class Storagelocation
    {
        /// <summary>
        /// TES点位
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// TES巷道号
        /// </summary>
        public string Originalcode { get; set; }

        /// <summary>
        /// WMS货位地址
        /// </summary>
        public string Wmsreference { get; set; }

        /// <summary>
        /// 容器号
        /// </summary>
        public string PodID { get; set; }   // 新增
    }
}
