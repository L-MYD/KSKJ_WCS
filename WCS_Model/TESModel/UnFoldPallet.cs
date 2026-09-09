using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class UnFoldPallet : ToTesPublicParameters
    {
        /// <summary>
        /// 拆牌站点号
        /// </summary>
        public string desStationCode { get; set; }

        /// <summary>
        /// 拆托盘数量：0 ：整垛，1：单托
        /// </summary>
        public int podNum { get; set; }

        /// <summary>
        /// 托盘码
        /// </summary>
        public string podID { get; set; }  //等待TES更新

        /// <summary>
        /// 站点类型：6-固定传6
        /// </summary>
        public int desType { get; set; } = 6;
    }
}
