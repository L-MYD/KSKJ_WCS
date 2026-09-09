using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class AddPodModel : ToTesPublicParameters
    {
        /// <summary>
        /// 容器列表
        /// </summary>
        [JsonProperty("podInfo")]
        public string PadInfoList { get; set; }
    }

    public class PadInfo
    {
        /// <summary>
        /// 容器号
        /// </summary>
        [JsonProperty("podID")]
        public string PodID { get; set; }

        /// <summary>
        /// 位置类型：0 储位 99 库外
        /// </summary>
        [JsonProperty("positionType")]
        public int PositionType { get; set; } = 0;

        /// <summary>
        /// 储位号
        /// </summary>
        [JsonProperty("storeID")]
        public string StoreID { get; set; }

        ///// <summary>
        ///// 容器类型：2 托盘
        ///// </summary>
        //[JsonProperty("podType")]
        //public int PodType { get; set; } = 2;
    }
}
