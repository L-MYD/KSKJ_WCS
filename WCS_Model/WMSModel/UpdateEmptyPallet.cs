using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    public class UpdateEmptyPallet
    {
        /// <summary>事务ID，固定 "WWCSAPI"</summary>
        public string trxId { get; set; } = "WWCSAPI";

        /// <summary>内层消息 JSON 字符串</summary>
        public string strInMsg { get; set; }
    }

    /// <summary>
    /// 请求空托盘出库的内层数据（对应 strInMsg 的内容）
    /// </summary>
    public class UpdateEmptyPalletData
    {
        /// <summary>事务ID，固定 "bisData"</summary>
        public string trxId { get; set; } = "WWCSAPI";

        /// <summary>操作标志，固定 "S"</summary>
        public string actionFlg { get; set; } = "K";

        /// <summary>叠盘机设备号</summary>
        public string carrierLoc { get; set; }

        /// <summary>操作人</summary>
        public string evtUsr { get; set; } = "WCS";
    }
}
