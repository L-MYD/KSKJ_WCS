using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WCS 调用 WMS 更新任务状态的请求模型
    /// </summary>
    public class UpdateTaskRequest
    {
        /// <summary>事务ID，固定 "WWCSAPI"</summary>
        public string trxId { get; set; } = "WWCSAPI";

        /// <summary>内层消息 JSON 字符串</summary>
        public string strInMsg { get; set; }
    }

    /// <summary>
    /// 更新任务状态的内层数据（对应 strInMsg 的内容）
    /// </summary>
    public class UpdateTaskInnerData
    {
        /// <summary>事务ID，固定 "bisData"</summary>
        public string trxId { get; set; } = "bisData";

        /// <summary>操作标志，固定 "S"</summary>
        public string actionFlg { get; set; } = "S";

        /// <summary>任务唯一码（WMS下发的任务ID）</summary>
        public string taskId { get; set; }

        /// <summary>任务状态：1=开始 2=正常结束 3=空取 4=双存储 5=其它异常</summary>
        public string taskStat { get; set; }

        /// <summary>任务执行异常时上报信息</summary>
        public string errMsg { get; set; }

        /// <summary>操作人</summary>
        public string evtUsr { get; set; } = "WCS";

        /// <summary>请求编号（流水号）</summary>
        public string reqCode { get; set; }

        /// <summary>请求时间，格式 "yyyy-MM-dd HH:mm:ss"</summary>
        public string reqTime { get; set; }
    }
}
