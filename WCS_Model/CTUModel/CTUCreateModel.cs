using System;
using System.Collections.Generic;

namespace WCS_Models.CTUModel
{
    namespace WCS_Models.CTUModel
    {
        /// <summary>
        /// CTU创建任务模型
        /// </summary>
        [Serializable]
        public class CTUCreateModel
        {
            /// <summary>
            /// 任务类型
            /// </summary>
            public string taskType { get; set; }

            /// <summary>
            /// 任务组号
            /// </summary>
            public string taskGroupCode { get; set; }

            /// <summary>
            /// 任务组优先级
            /// </summary>
            public int groupPriority { get; set; }

            /// <summary>
            /// 任务列表
            /// </summary>
            public List<CTUCreatetaskModel> tasks { get; set; }
        }

        /// <summary>
        /// 任务信息
        /// </summary>
        [Serializable]
        public class CTUCreatetaskModel
        {
            /// <summary>
            /// 任务编码
            /// </summary>
            public string taskCode { get; set; }

            /// <summary>
            /// 任务优先级
            /// </summary>
            public int taskPriority { get; set; }

            /// <summary>
            /// 任务描述
            /// </summary>
            public CTUtaskDescribe taskDescribe { get; set; }
        }

        /// <summary>
        /// 任务描述
        /// </summary>
        [Serializable]
        public class CTUtaskDescribe
        {

            /// <summary>
            /// 容器编码
            /// </summary>
            public string containerCode { get; set; }


            /// <summary>
            /// 容器类型
            /// </summary>
            public string containerType { get; set; }

            /// <summary>
            /// 容器标签
            /// </summary>
            public string storageTag { get; set; }
        }

        /// <summary>
        /// 动作信息
        /// </summary>
        [Serializable]
        public class action
        {
            /// <summary>
            /// 容器编码
            /// </summary>
            public string containerCode { get; set; }

            /// <summary>
            /// 目标工作位编码
            /// </summary>
            public string toLocationCode { get; set; }

            /// <summary>
            /// 等待时间（毫秒）
            /// </summary>
            public int? waitTimeMs { get; set; }
        }

        /// <summary>
        /// CTU创建任务响应模型（最外层）
        /// </summary>
        [Serializable]
        public class CTUCreateResponse
        {
            /// <summary>
            /// 响应状态码
            /// 0：表示处理成功
            /// 其他值：表示处理失败，详见异常码
            /// </summary>
            public int code { get; set; }

            /// <summary>
            /// 响应消息
            /// 成功时为"success"，失败时为错误描述
            /// </summary>
            public string msg { get; set; }

            /// <summary>
            /// 响应数据
            /// </summary>
            public CTUCreateResponseData data { get; set; }
        }

        /// <summary>
        /// CTU创建任务响应数据
        /// </summary>
        [Serializable]
        public class CTUCreateResponseData
        {
            /// <summary>
            /// 任务结果列表
            /// </summary>
            public List<CTUTaskResult> Tasks { get; set; }
        }

        /// <summary>
        /// CTU单个任务结果
        /// </summary>
        [Serializable]
        public class CTUTaskResult
        {
            /// <summary>
            /// 响应状态码
            /// 0：表示处理成功
            /// 其他值：表示处理失败，详见异常码
            /// </summary>
            public string errorCode { get; set; }

            /// <summary>
            /// 响应消息补充说明
            /// 当errorCode为0时，message值为OK
            /// 当errorCode为其他值时，message值为错误码对应的错误描述
            /// </summary>
            public string message { get; set; }

            /// <summary>
            /// 任务编码
            /// </summary>
            public string taskCode { get; set; }
        }
    }
}