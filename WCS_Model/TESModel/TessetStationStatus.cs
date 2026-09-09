using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class TessetStationStatus : ToTesPublicParameters
    {
        /// <summary>
        /// 站点编号
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 站点状态 出入库流向- 11 出库- 12 入库- 1 禁用
        /// </summary>
        public int status { get; set; }

        /// <summary>
        /// 0 默认  1 tes不下发IM/OM指令
        /// </summary>
        public int ignoreChangeFlowCmd { get; set; }
    }

    public class TessetStationStatusReturn
    {

        /// <summary>
        /// 站点编号
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 站点状态 出入库流向- 11 出库- 12 入库- 1 禁用
        /// </summary>
        public int status { get; set; }

        /// <summary>
        /// 切换状态：- 0 站点操作状态完成- 1 站点操作状态进行中 - 2 站点操作状态已下发指令
        /// </summary>
        public int operationStatus { get; set; }
    }
}
