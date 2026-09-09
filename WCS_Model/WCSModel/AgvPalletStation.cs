using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class AgvPalletStation
    {
        public  int Id {  get; set; }

        /// <summary>
        /// PS站点编号
        /// </summary>
        public string PSStationCode { get; set; }


        /// <summary>
        /// AGV站点编号
        /// </summary>
        public string AGVStationCode { get; set; }

        /// <summary>
        /// 站点区域名称
        /// </summary>
        public string StationName { get; set; }

        /// <summary>
        /// 站点状态 出入库流向- 11 出库- 12 入库- 1 禁用
        /// </summary>
        public int StationStatu { get; set; }

        /// <summary>
        /// 占用状态- 1 预占用- 2 托盘占用- 3 无占用
        /// </summary>
        public int OccupyStatus { get; set; }

        /// <summary>
        /// 容器号
        /// </summary>
        public string Podid {  get; set; }
    }
}
