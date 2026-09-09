using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WCS托盘入库申请 WMS：WCS->WMS  模型
    /// </summary>
    public class PalnoApplyModel
    {

        /// <summary>
        /// WCSID唯一请求号
        /// </summary>
        public string reqCode { get; set; }

        /// <summary>
        /// 请求时间  格式：yyyy-MM-dd HH:mm:ss
        /// </summary>
        public string reqTime { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 设备，PLC设备等
        /// </summary>
        public string eqptId { get; set; }

        /// <summary>
        /// 当前位置(出入库接驳口，或货物移障标识)
        ///移障标识:YZFLG
        /// </summary>
        public string carrierLoc { get; set; }


        /// <summary>
        /// 容器号
        /// </summary>
        public string carrierId { get; set; }

        /// <summary>
        /// 操作人
        /// </summary>
        public string evtUsr { get; set; } = "WCS";

        /// <summary>
        /// 重量
        /// </summary>
        public string weight { get; set; }

        /// <summary>
        /// 叠盘机上母托数量 8
        /// </summary>
        public int prdQty { get; set; }

        /// <summary>
        /// 叠盘机入库标识:FOLD
        /// </summary>
        public string carrierDpj { get; set; }

    }
}
