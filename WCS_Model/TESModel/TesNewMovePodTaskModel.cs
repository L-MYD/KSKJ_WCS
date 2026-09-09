using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class TesNewMovePodTaskModel : ToTesPublicParameters
    {
        /// <summary>
        /// 优先级，范围(1-9)，9是最高
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 搬运类型，固定传1
        /// </summary>
        public int SrcType { get; set; }

        /// <summary>
        ///  容器号
        /// </summary>
        public string PodID { get; set; }

        /// <summary>
        /// 业务ID
        /// </summary>
        public string BizID { get; set; }

        /// <summary>
        /// 是否替换该容器的其他任务
        /// </summary>
        public int ReplacePodTask { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// 目标类型
        /// </summary>
        public int DesType { get; set; }

        /// <summary>
        /// 目标导航点ID
        /// </summary>
        public string DesNodeID { get; set; }

        /// <summary>
        /// 目标储位号
        /// </summary>
        public string DesStorageID { get; set; }

        /// <summary>
        /// 目标区域编号
        /// </summary>
        public string DesZoneCode { get; set; }

        /// <summary>
        /// 目标站点列表
        /// </summary>
        public string DesStationCodes { get; set; }

        /// <summary>
        /// 储位选择偏好
        /// </summary>
        public StoragePreference StoragePreference { get; set; }

        /// <summary>
        /// 任务扩展参数
        /// </summary>
        public TaskExt TaskExt { get; set; }

        /// <summary>
        /// 终点扩展参数
        /// </summary>
        public DesExt DesExt { get; set; }

        /// <summary>
        /// 业务扩展参数
        /// </summary>
        public BizExt BizExt { get; set; }
    }

    public class StoragePreference
    {
        /// <summary>
        /// 候选位置
        /// </summary>
        public List<string> CandidateStorageIDs { get; set; }

        /// <summary>
        /// 物料类型
        /// </summary>
        public string MaterialClass { get; set; }
    }

    public class TaskExt
    {
        ///// <summary>
        ///// 指定运力组中的小车搬运
        ///// </summary>
        //public string RobotGroupID { get; set; }

        ///// <summary>
        ///// 顶盘旋转模式
        ///// </summary>
        //public int TurnMode { get; set; }

        ///// <summary>
        ///// 最大车速(mm/s)
        /////  </summary>
        //public int MaxSpeed { get; set; }

        ///// <summary>
        ///// 任务超时失败的时间(秒)
        ///// </summary>
        //public int TimeoutFailed { get; set; }

        /// <summary>
        /// 小车搬运完成后的行为
        /// </summary>
        public int AutoToRest { get; set; }

        ///// <summary>
        ///// 小车搬运完成后的行为
        ///// </summary>
        //public int KeepRobot { get; set; }

        ///// <summary>
        ///// 保持原地的最长时长(分钟)
        ///// </summary>
        //public int KeepRobotTimeout { get; set; }

        ///// <summary>
        ///// 叠盘类型
        ///// </summary>
        //public int FoldType { get; set; }

        ///// <summary>
        ///// 叠盘数量
        ///// </summary>
        //public int FoldNum { get; set; }

        ///// <summary>
        ///// 是否进行外形检测
        ///// </summary>
        //public int Check { get; set; }

        ///// <summary>
        ///// 是否去覆膜任务
        ///// </summary>
        //public int Cover { get; set; }

        ///// <summary>
        ///// 是否去缠膜任务
        ///// </summary>
        //public int Wrap { get; set; }

        ///// <summary>
        ///// 外检给plc下发货型信息
        ///// </summary>
        //public string CargoType { get; set; }

        ///// <summary>
        ///// 如果目标站点禁用，如何处理任务
        ///// </summary>
        //public string WhenTargetDisabled { get; set; }

        ///// <summary>
        ///// 保留
        ///// </summary>
        //public int DelayDispatch { get; set; }
    }

    public class DesExt
    {
        /// <summary>
        /// 是否放下容器
        /// </summary>
        public int Unload { get; set; }

        /// <summary>
        /// 小车放下容器后的姿态朝向
        /// </summary>
        //public float RobotFace { get; set; }

        /// <summary>
        /// 容器搬运到位的姿态朝向
        /// </summary>
        //public float PodFace { get; set; }
    }

    public class BizExt
    {
        /// <summary>
        /// 业务类型
        /// </summary>
        public string BizType { get; set; }

        /// <summary>
        /// 出库波次
        /// </summary>
        public string WaveID { get; set; } 
    }

}
