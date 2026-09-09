using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.CTUModel
{
    public class CTUCancelModel
    {
        /// <summary>
        /// 任务编码。业务任务和系统任务都可取消。
        /// </summary>
        public List<string> taskCodes {  get; set; }
    }

    /// <summary>
    /// CTU取消任务响应模型（最外层）
    /// </summary>
    [Serializable]
    public class CTUCancelResponse
    {
        /// <summary>
        /// 响应状态码
        /// 0：表示处理成功
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// 响应消息
        /// 成功时为"success"
        /// </summary>
        public string msg { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public Canceldata data { get; set; }
    }

    /// <summary>
    /// CTU取消任务响应数据
    /// </summary>
    [Serializable]
    public class Canceldata
    {
        /// <summary>
        /// 任务结果列表
        /// </summary>
        public List<taskResult> tasks { get; set; }
    }

    /// <summary>
    /// CTU单个任务结果
    /// </summary>
    [Serializable]
    public class taskResult
    {
        /// <summary>
        /// 响应状态码
        /// 0：表示处理成功
        /// </summary>
        public string errorCode { get; set; }

        /// <summary>
        /// 响应消息补充说明
        /// 当errorCode为0时，message值为OK
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 任务编码
        /// </summary>
        public string taskCode { get; set; }
    }
}
