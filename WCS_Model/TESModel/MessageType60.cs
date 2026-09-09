using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class MessageType60
    {
        /// <summary>
        /// 容器号
        /// </summary>
        public string podID { get; set; }

        /// <summary>
        /// 站点号
        /// </summary>
        public string stationCode {  get; set; }
        
        /// <summary>
        /// 占用状态 1 预占用 2 托盘占用 3 无占用
        /// </summary>
        public int occupyStatus { get; set; }
    }

    public class MessageType61
    {
        /// <summary>
        /// 站点编号
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 站点状态 出入库流向- 11 出库- 12 入库- 1 禁用
        /// </summary>
        public int status {  get; set; }

        /// <summary>
        /// 切换状态：- 0 切换完成- 1 切换中
        /// </summary>
        public int operationStatus { get; set; }

        /// <summary>
        /// 站点交互状态- 1上料就绪- 2下料就绪- 3上料完成- 4下料完成- 0非外部接驳站点该值为0
        /// </summary>
        public int interactiveStatus { get; set; }
    }

    public class MessageType72
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public int taskID { get; set; }

        /// <summary>
        /// 业务ID
        /// </summary>
        public string bizID { get; set; }

        /// <summary>
        /// 储位号
        /// </summary>
        public string nodeCode { get; set; }

        /// <summary>
        /// 容器号
        /// </summary>
        public string podID { get; set; }

        /// <summary>
        /// 站点号
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 小车号
        /// </summary>
        public string robotID { get; set; }
    }

    public class MessageType51
    {
        /// <summary>
        /// 站点号
        /// </summary>
        public string stationCode { get; set; }

        /// <summary>
        /// 设备号
        /// </summary>
        public string robotID { get; set; }

        /// <summary>
        /// 托盘数量
        /// </summary>
        public int podNum { get; set; }
    }

    public class MessageType100
    {
        /// <summary>
        /// 库区号
        /// </summary>
        public string regionCode { get; set; }

        /// <summary>
        /// 储位编号列表
        /// </summary>
        public string[] storageCode { get;set; }

        /// <summary>
        /// 状态值 0: 解除禁止通行 1: 禁止通行
        /// </summary>
        public int disable { get; set; }
    }
}
