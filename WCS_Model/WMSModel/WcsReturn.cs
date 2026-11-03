using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WMS 任务下发接口（/api/WCS/WMSTaskDis）返回参数
    /// </summary>
    public class WcsReturn
    {
        /// <summary>
        /// 消息类型  示例：S:成功  E:程序异常  F:失败
        /// </summary>
        public string msgType { get; set; }

        /// <summary>
        /// 消息文本
        /// </summary>
        public string msgText { get; set; }

        /// <summary>
        /// WMS 流水号（协议规定返回 WmsId）
        /// </summary>
        public string WmsId { get; set; }
    }
}
