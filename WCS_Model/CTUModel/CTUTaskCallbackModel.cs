using System;
using System.Collections.Generic;

namespace WCS_Models.CTUModel
{
    /// <summary>
    /// 任务状态回调模型
    /// </summary>
    [Serializable]
    public class CTUTaskCallbackModel
    {
        /// <summary>
        /// 一次回调唯一标识
        /// </summary>
        public string callId { get; set; }

        /// <summary>
        /// 业务任务号
        /// 当eventType为robot_reach时不填
        /// 当eventType为其他值时必填
        /// </summary>
        public string taskCode { get; set; }

        /// <summary>
        /// 上报事件类型：
        /// task：上报任务状态发生变化的事件
        /// task_allocated: 上报任务分配给机器人
        /// tote_load：上报取箱状态
        /// tote_unload：上报放箱状态
        /// robot_reach：机器人到达工作站
        /// tote_scan：机器人扫码
        /// </summary>
        public string eventType { get; set; }

        /// <summary>
        /// 任务状态：
        /// success：成功
        /// fail：失败
        /// cancel：取消
        /// suspend：挂起
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 机器人编码
        /// </summary>
        public string robotCode { get; set; }

        /// <summary>
        /// 容器编码
        /// </summary>
        public string containerCode { get; set; }

        /// <summary>
        /// 工作位编码
        /// </summary>
        public string locationCode { get; set; }

        /// <summary>
        /// 工作站编码
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 描述信息，描述异常原因/挂起原因等
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 系统任务编码，用于任务挂起回调
        /// 可通过系统任务编码恢复挂起的系统任务
        /// 业务任务没有挂起状态
        /// </summary>
        public string sysTaskCode { get; set; }

        /// <summary>
        /// 库位是否有容器，当eventType为task且为盘点任务时才会返回该值
        /// </summary>
        public bool? isLocationHasContainer { get; set; }

        /// <summary>
        /// 背篓层号，从0层开始，从下往上编号
        /// 64表示放在了货叉上
        /// 当eventType为task且为称重盘点任务或rfid盘点任务时才会返回该值
        /// </summary>
        public int? trayLevel { get; set; }

        /// <summary>
        /// 重量，单位为g
        /// 当eventType为task且为称重盘点任务才会返回该值
        /// </summary>
        public int? weight { get; set; }

        /// <summary>
        /// rfid盘点信息
        /// 当eventType为task且为rfid盘点任务时才会返回该值
        /// </summary>
        public List<string> rfidInfo { get; set; }

        /// <summary>
        /// 机器人类型编码
        /// 当eventType为robot_reach时才会返回该值
        /// </summary>
        public string robotTypeCode { get; set; }

        /// <summary>
        /// 背篓信息
        /// 当eventType为robot_reach时才会返回该值
        /// </summary>
        public List<TrayItem> trays { get; set; }
    }

    /// <summary>
    /// 背篓信息
    /// </summary>
    [Serializable]
    public class TrayItem
    {
        /// <summary>
        /// 容器编码
        /// </summary>
        public string containerCode { get; set; }

        /// <summary>
        /// 背篓层号，从0层开始，从下往上编号
        /// 64表示容器放在了货叉上
        /// </summary>
        public int trayLevel { get; set; }

        /// <summary>
        /// 位置编码，由机器人编码和背篓层数组成，即机器人id#背篓层数
        /// </summary>
        public string positionCode { get; set; }

        /// <summary>
        /// 目标容器朝向，取值为枚举值A、B、C、D
        /// 常用于出库HAIFLEX需要翻面的场景
        /// A：0, B：90, C：180, D：270
        /// </summary>
        public string containerFace { get; set; }
    }
}