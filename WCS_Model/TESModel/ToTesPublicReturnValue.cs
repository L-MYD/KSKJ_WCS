using WCS_Models.TESModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models
{
    /// <summary>
    /// WCS TO Tes接口调用公共返回参数
    /// </summary>
    public class ToTesPublicReturnValue
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        /// 错误信息（英文）
        /// </summary>
        public string ReturnMsg { get; set; }

        /// <summary>
        /// 错误信息（多国语言）
        /// </summary>
        public string ReturnUserMsg { get; set; }
    }

    public class newMovePodTaskReturn : ToTesPublicReturnValue
    {
        public string TaskID { get; set; }
    }

    public class StationReturn : ToTesPublicReturnValue
    {
        public TessetStationStatusReturn data { get; set; }
    }

    public class GetStationReturn : ToTesPublicReturnValue
    {
        public List<StationInfo> data { get; set; }
    }
    public class StationInfo
    {
        /// <summary>
        /// 站点编码
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 站点状态:0 启用、1 禁用
        /// </summary>
        public int status { get; set; }

        /// <summary>
        /// 站点操作状态:0操作完成、1操作中
        /// </summary>
        public int operationStatus { get; set; }

        /// <summary>
        /// 站点容器ID
        /// </summary>
        public string podID { get; set; }

        /// <summary>
        /// 站点是否被占用：0 启用、1 占用(站点有任务、站点有容器都会返回占用状态)
        /// </summary>
        public int isOccupied { get; set; }

        /// <summary>
        /// 站点占用状态
        /// </summary>
        public int occupyStatus { get; set; }
    }

    public class setStationStatus : ToTesPublicParameters
    {
        /// <summary>
        /// 站点编码列表(逗号分隔字符串)
        /// </summary>
        public string stationCodes { get; set; }
    }

    public class SetStationInteractiveStatus : ToTesPublicParameters
    {
        /// <summary>
        /// 站点编码列表(逗号分隔字符串)
        /// </summary>
        public string stationCodes { get; set; }

        /// <summary>
        /// 站点交互状态 - 1上料就绪 - 2下料就绪 - 3上料完成 -4下料完成
        /// </summary>
        public int interactiveStatus { get; set; }
    }
}
