using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class Aisles
    {
        public int Id { get; set; }
        /// <summary>
        /// 通道名称
        /// </summary>
        public string AislesName {  get; set; }

        /// <summary>
        /// WMS货位号
        /// </summary>
        public string WmsReference { get; set; }

        /// <summary>
        /// 层号
        /// </summary>
        public string FloorNum { get; set; }

    }
}
