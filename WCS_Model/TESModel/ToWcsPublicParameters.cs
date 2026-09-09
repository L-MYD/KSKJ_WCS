
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WCS_Models.TESModel;

namespace WCS_Models.TESModel
{
    /// <summary>
    /// Tes to WCS回调公共参数
    /// </summary>
    public class ToWcsPublicParameters
    {
        /// <summary>
        /// 仓库ID
        /// </summary>
        [JsonProperty("warehouseID")]
        public string WarehouseID { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        [JsonProperty("messageType")]
        public int MessageType { get; set; }

        /// <summary>
        /// 消息号，唯一幂等校验
        /// </summary>
        [JsonProperty("messageID")]
        public long MessageID { get; set; }

        /// <summary>
        /// 消息产生时间(RFC3339)
        /// </summary>
        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        /// <summary>
        /// 消息具体内容
        /// </summary>
        [JsonProperty("content")]
        public object Content { get; set; }
    }

    /// <summary>
    /// 外检消息内容主体（可包含任务详情）
    /// </summary>
    public class InspectionContent
    {
        /// <summary>
        /// 检测设备编号（如："3003"）
        /// </summary>
        [JsonProperty("robotID")]
        public string RobotId { get; set; } = string.Empty;

        /// <summary>
        /// 站点编号（如："P-CHECK-1"）
        /// </summary>
        [JsonProperty("stationCode")]
        public string StationCode { get; set; } = string.Empty;

        /// <summary>
        /// 检测信号数据
        /// </summary>
        [JsonProperty("signal")]
        public InspectionSignal Signal { get; set; } = new InspectionSignal();
    }

    /// <summary>
    /// 外检结果数据
    /// </summary>
    public class InspectionResult
    {
        /// <summary>
        /// 虚拟托盘号（如："46000185"）
        /// </summary>
        [JsonProperty("podID")]
        public string PodId { get; set; } = string.Empty;

        /// <summary>
        /// 检测信号数据
        /// </summary>
        [JsonProperty("signal")]
        public InspectionSignal Signal { get; set; } = new InspectionSignal();

        /// <summary>
        /// 错误状态（预留字段，通常为null）
        /// </summary>
        [JsonProperty("errorState")]
        public object ErrorState { get; set; }

        /// <summary>
        /// 错误信息（预留字段，通常为null）
        /// </summary>
        [JsonProperty("errorMessage")]
        public object ErrorMessage { get; set; }
    }

    /// <summary>
    /// 检测信号数据
    /// </summary>
    public class InspectionSignal
    {
        /// <summary>
        /// 设备工位号（如："3003"）
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 错误代码（0=成功，非0=错误）
        /// 已知错误码：
        /// 256 - 托盘号未提前加入TES系统
        /// 512 - 容器编号已存在
        /// 1024 - 托盘外检高度为0
        /// </summary>
        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// 错误原因列表（errorCode=0时为空数组）
        /// </summary>
        [JsonProperty("errorReason")]
        public List<string> ErrorReasons { get; set; } = new List<string>();

        /// <summary>
        /// 站点类型（如："sizeCheck"表示尺寸检查）
        /// </summary>
        [JsonProperty("type")]
        public string CheckType { get; set; } = string.Empty;

        /// <summary>
        /// 托盘条码（如："90000525"）
        /// </summary>
        [JsonProperty("barCode")]
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 托盘类型（预留字段，示例中为0）
        /// </summary>
        [JsonProperty("podType")]
        public int PodType { get; set; }

        /// <summary>
        /// 信号体（包含具体检测数据）
        /// </summary>
        [JsonProperty("signalBody")]
        public SignalBody SignalBody { get; set; } = new SignalBody();
    }

    /// <summary>
    /// 检测信号体（包含具体测量数据）
    /// </summary>
    public class SignalBody
    {
        /// <summary>
        /// RFID数据（示例中为null）
        /// </summary>
        [JsonProperty("rfid")]
        public object Rfid { get; set; }

        /// <summary>
        /// 托盘重量（单位：克，如：1000）
        /// </summary>
        [JsonProperty("weight")]
        public int Weight { get; set; }

        /// <summary>
        /// 托盘重量（单位：千克，如：999.99）
        /// </summary>
        [JsonProperty("weightKG")]
        public float WeightKg { get; set; }

        /// <summary>
        /// 是否超重（0=正常，1=超重）
        /// </summary>
        [JsonProperty("weightOutOfRange")]
        public int WeightOutOfRange { get; set; }

        /// <summary>
        /// 尺寸检查数据
        /// </summary>
        [JsonProperty("sizeCheck")]
        public SizeCheck SizeCheck { get; set; } = new SizeCheck();

        /// <summary>
        /// 电气检查（预留字段，示例中为空字符串）
        /// </summary>
        [JsonProperty("electricalCheck")]
        public string ElectricalCheck { get; set; } = string.Empty;

        /// <summary>
        /// 按钮状态（预留字段，示例中为0）
        /// </summary>
        [JsonProperty("button")]
        public int Button { get; set; }
    }

    /// <summary>
    /// 尺寸检查数据
    /// </summary>
    public class SizeCheck
    {
        /// <summary>
        /// 托盘长度（单位：毫米，示例中为0）
        /// </summary>
        [JsonProperty("length")]
        public int Length { get; set; }

        /// <summary>
        /// 托盘宽度（单位：毫米，示例中为0）
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary>
        /// 托盘高度（单位：毫米，如：1550）
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>
        /// 是否超长（0=正常，1=超长）
        /// </summary>
        [JsonProperty("lengthOutOfRange")]
        public int LengthOutOfRange { get; set; }

        /// <summary>
        /// 是否超宽（0=正常，1=超宽）
        /// </summary>
        [JsonProperty("widthOutOfRange")]
        public int WidthOutOfRange { get; set; }

        /// <summary>
        /// 是否超高（0=正常，1=超高）
        /// </summary>
        [JsonProperty("heightOutOfRange")]
        public int HeightOutOfRange { get; set; }
    }

    /// <summary>
    /// 查询任务详情返回数据
    /// </summary>
    public class GetTaskDetail : ToTesPublicParameters
    {
        /// <summary>
        /// 响应数据
        /// </summary>
        [JsonProperty("data")]
        public TaskDataResponse Data { get; set; }
    }

    public class TaskDataResponse
    {
        /// <summary>
        /// 任务详情
        /// </summary>
        [JsonProperty("detail")]
        public TaskDetailModel Detail { get; set; }
    }

    /// <summary>
    /// 任务详情信息（整合版）
    /// </summary>
    public class TaskDetailModel
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        [JsonProperty("taskID")]
        public int TaskID { get; set; }

        /// <summary>
        /// 业务ID
        /// </summary>
        [JsonProperty("bizID")]
        public string BizID { get; set; }

        /// <summary>
        /// 业务类型
        /// </summary>
        [JsonProperty("bizType")]
        public string BizType { get; set; }

        /// <summary>
        /// 优先级 (1-最高，数字越大优先级越低)
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 任务类型 (参见任务类型枚举)
        /// </summary>
        [JsonProperty("taskType")]
        public int TaskType { get; set; }

        /// <summary>
        /// 任务状态 (1-就绪，参见状态枚举)
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>
        /// 错误码 (0表示无错误)
        /// </summary>
        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// 错误原因
        /// </summary>
        [JsonProperty("errorReason")]
        public string ErrorReason { get; set; }

        /// <summary>
        /// 机器人ID (任务分配后才有)
        /// </summary>
        [JsonProperty("robotID")]
        public string RobotID { get; set; }

        /// <summary>
        /// 容器/货架ID
        /// </summary>
        [JsonProperty("podID")]
        public string PodID { get; set; }

        /// <summary>
        /// 容器当前位置信息
        /// </summary>
        [JsonProperty("podInfo")]
        public PodInfo PodInfo { get; set; }

        /// <summary>
        /// 目标类型 (1-站点，2-储位，3-工作台...)
        /// </summary>
        [JsonProperty("desType")]
        public int DesType { get; set; }

        /// <summary>
        /// 目标导航点ID
        /// </summary>
        [JsonProperty("desNodeID")]
        public string DesNodeID { get; set; }

        /// <summary>
        /// 目标篮子ID (拣选任务使用)
        /// </summary>
        [JsonProperty("desBasketID")]
        public int DesBasketID { get; set; }

        /// <summary>
        /// 目标储位ID
        /// </summary>
        [JsonProperty("desStorageID")]
        public string DesStorageID { get; set; }

        /// <summary>
        /// 目标站点编码
        /// </summary>
        [JsonProperty("desStationCodes")]
        public string DesStationCodes { get; set; }

        /// <summary>
        /// 目标区域编码
        /// </summary>
        [JsonProperty("desZoneCode")]
        public string DesZoneCode { get; set; }

        /// <summary>
        /// 任务创建时间 (格式: yyyy-MM-dd HH:mm:ss)
        /// </summary>
        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        /// <summary>
        /// 任务完成时间 (默认值表示未完成)
        /// </summary>
        [JsonProperty("finishTime")]
        public string FinishTime { get; set; }

        /// <summary>
        /// 任务执行结果
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }

        /// <summary>
        /// 仓库编号
        /// </summary>
        [JsonProperty("warehouseID")]
        public string WarehouseID { get; set; }

        /// <summary>
        /// 客户端标识
        /// </summary>
        [JsonProperty("clientCode")]
        public string ClientCode { get; set; }
    }

    public class PodInfo
    {
        /// <summary>
        /// 位置类型 (0-未知，1-导航点，2-储位...)
        /// </summary>
        [JsonProperty("positionType")]
        public int PositionType { get; set; }

        /// <summary>
        /// 当前位置节点ID
        /// </summary>
        [JsonProperty("nodeID")]
        public string NodeID { get; set; }
    }

    /// <summary>
    /// 容器位置信息
    /// </summary>
    //public class PodInfo
    //{
    //    /// <summary>
    //    /// 容器位置类型
    //    /// </summary>
    //    [JsonProperty("positionType")]
    //    public int PositionType { get; set; }

    //    /// <summary>
    //    /// 容器位置编号
    //    /// </summary>
    //    [JsonProperty("nodeID")]
    //    public string NodeId { get; set; } = string.Empty;
    //}
}
