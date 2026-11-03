using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WMS任务下发模型（字段与 WHSF_WCS_Api 保持一致）
    /// </summary>
    public class WMSTaskDisModel
    {
        /// <summary>
        /// WMS 的流水号
        /// </summary>
        public string WmsId { get; set; }

        /// <summary>
        /// 托盘号
        /// </summary>
        public string Palno { get; set; }

        /// <summary>
        /// 起始地
        /// </summary>
        public string FromLocation { get; set; }

        /// <summary>
        /// 目的地
        /// </summary>
        public string ToLocation { get; set; }

        /// <summary>
        /// 是否拣选 0：整盘 1：拣选
        /// </summary>
        public string IfPicking { get; set; }

        /// <summary>
        /// 优先级 数字越大越优先，整数
        /// </summary>
        public string Num { get; set; }

        /// <summary>
        /// 任务类型 01入成品库 02入灭菌缓存 03灭菌 04入解析区 05无任务组(PS) 06无任务组(AGV)
        /// </summary>
        public string TaskType { get; set; }
    }

    /// <summary>
    /// WMS任务下发临时表模型
    /// </summary>
    public class WMSTaskDis
    {
        /// <summary>
        /// WMS 的流水号
        /// </summary>
        public string WmsId { get; set; }

        /// <summary>
        /// WCS 的流水号
        /// </summary>
        public string WcsId { get; set; }

        /// <summary>
        /// 托盘号
        /// </summary>
        public string Palno { get; set; }

        /// <summary>
        /// 起始地
        /// </summary>
        public string FromLocation { get; set; }

        /// <summary>
        /// 目的地
        /// </summary>
        public string ToLocation { get; set; }

        /// <summary>
        /// 是否拣选 0：整盘 1：拣选
        /// </summary>
        public string IfPicking { get; set; }

        /// <summary>
        /// 优先级 数字越大越优先，整数
        /// </summary>
        public string Num { get; set; }

        /// <summary>
        /// 任务类型 01入成品库 02入灭菌缓存 03灭菌 04入解析区 05无任务组(PS) 06无任务组(AGV)
        /// </summary>
        public string TaskType { get; set; }
    }
}
