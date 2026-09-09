using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class TaskInfo
    {
        /// <summary>
        /// 任务发送方，SUPER表示运维后台发送
        /// </summary>
        [JsonProperty("clientCode")]
        public string ClientCode { get; set; }

        /// <summary>
        /// 任务ID
        /// </summary>
        [JsonProperty("taskID")]
        public string TaskID { get; set; }

        /// <summary>
        /// 业务ID
        /// </summary>
        [JsonProperty("bizID")]
        public string BizID { get; set; }

        /// <summary>
        /// 任务优先级(1-99)
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 任务类型(见附录)
        /// </summary>
        [JsonProperty("taskType")]
        public int TaskType { get; set; }

        /// <summary>
        /// 任务状态(见附录)
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>
        /// 任务失败错误码(见附录)
        /// </summary>
        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// 任务失败信息(见附录)
        /// </summary>
        [JsonProperty("errorReason")]
        public string ErrorReason { get; set; }

        /// <summary>
        /// 最后一个执行设备
        /// </summary>
        [JsonProperty("robotID")]
        public string RobotID { get; set; }

        /// <summary>
        /// 容器号
        /// </summary>
        [JsonProperty("podID")]
        public string PodID { get; set; }

        /// <summary>
        /// 目的地类型，同任务下发参数
        /// </summary>
        [JsonProperty("desType")]
        public int DesType { get; set; }

        /// <summary>
        /// 目标点ID
        /// </summary>
        [JsonProperty("desNodeID")]
        public string DesNodeID { get; set; }

        /// <summary>
        /// 目标储位ID
        /// </summary>
        [JsonProperty("desStorageID")]
        public string DesStorageID { get; set; }

        /// <summary>
        /// 目标区域ID
        /// </summary>
        [JsonProperty("desZoneCode")]
        public string DesZoneCode { get; set; }

        /// <summary>
        /// 目标站点编号
        /// </summary>
        [JsonProperty("desStationCodes")]
        public string DesStationCodes { get; set; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        /// <summary>
        /// 任务完成时间
        /// </summary>
        [JsonProperty("finishTime")]
        public string FinishTime { get; set; }

        /// <summary>
        /// 容器位置信息
        /// </summary>
        [JsonProperty("podInfo")]
        public PodInfo PodInfo { get; set; }
    }

    public class getTaskDetail : ToTesPublicParameters
    {
        /// <summary>
        /// Tes任务ID
        /// </summary>
        public string taskID { get; set; }
    }
}
