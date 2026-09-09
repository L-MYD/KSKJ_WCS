using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WMS任务下发模型
    /// </summary>
    public class newMoveTaskModel
    {
        /// <summary>
        /// WMS任务号
        /// </summary>
        public string wmsId { get; set; }

        /// <summary>
        /// 容器号(允许字母）
        /// </summary>
        public string podId { get; set; }

        /// <summary>
        /// 起始位置
        /// </summary>
        public string currentPosition { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public string desPosition { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public string wmsTaskMessType { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int priority { get; set; } = 3;
    }
}
