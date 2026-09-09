using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class retryTaskModel
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string taskID { get; set; }
    }

    public class TESretryTaskModel : ToTesPublicParameters
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public int taskID { get; set; }
    }

    public class reMovePodTaskModel
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string taskID { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public string destPosition { get; set; }
    }
}
