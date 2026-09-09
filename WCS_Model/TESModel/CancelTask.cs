using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class CancelTask : ToTesPublicParameters
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public int taskID { get; set; }

        /// <summary>
        /// 取消原因
        /// </summary>
        public string reason { get; set; }

        /// <summary>
        ///  0 默认取消  1 强制取消
        /// </summary>
        public int force { get; set; }

        /// <summary>
        /// 0 默认取消 1 已经开始执行，则不取消
        /// </summary>
        public int withoutRunning { get; set; }
    }

    public class CancelTaskToWMS
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string taskID { get; set; }

        /// <summary>
        /// 取消原因
        /// </summary>
        public string reason { get; set; }

        /// <summary>
        ///  0 默认取消  1 强制取消
        /// </summary>
        public int force { get; set; }

        /// <summary>
        /// 0 默认取消 1 已经开始执行，则不取消
        /// </summary>
        public int withoutRunning { get; set; }
    }
}
