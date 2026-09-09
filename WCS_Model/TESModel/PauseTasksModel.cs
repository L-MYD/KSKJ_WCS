using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class PauseTasksModel : ToTesPublicParameters
    {
        /// <summary>
        /// 业务号列表（两个参数必传一个）
        /// </summary>
        //public string bizIDs { get; set; }

        /// <summary>
        /// 任务号列表
        /// </summary>
        public string taskIDs { get; set; }
    }

    public class PauseTasksModelToWMS : ToTesPublicParameters
    {
        /// <summary>
        /// WMS任务号
        /// </summary>
        public string taskID { get; set; }
    }
}
