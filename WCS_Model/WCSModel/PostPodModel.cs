using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WCSModel
{
    public class PostPodModel
    {
        /// <summary>
        /// WMS任务号
        /// </summary>
        public string taskId { get; set; }

        /// <summary>
        /// 容器号(允许字母）
        /// </summary>
        public string containerNo { get; set; }

        /// <summary>
        /// 当前站点
        /// </summary>
        public string currentPosition { get; set; }

        /// <summary>
        /// 容器高度
        /// </summary>
        public int podHeight { get; set; }
    }
}
