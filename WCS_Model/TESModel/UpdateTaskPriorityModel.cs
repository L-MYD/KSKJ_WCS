using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class UpdateTaskPriorityModel : ToTesPublicParameters
    {
        /// <summary>
        /// 任务号
        /// </summary>
        public int taskID { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int priority { get; set; }
    }

    public class UpdateTaskPriorityModelToWMS : ToTesPublicParameters
    {
        /// <summary>
        /// WMS任务号
        /// </summary>
        public string taskID { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int priority { get; set; }
    }
}
