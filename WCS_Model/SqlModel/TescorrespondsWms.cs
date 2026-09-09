using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WCS_Models.SqlModel
{
    public class TescorrespondsWms
    {
        /// <summary>
        /// 唯一自增
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        /// <summary>
        /// WCS任务ID
        /// </summary>
        [Column("wcs_id")]
        public string WcsId { get; set; }

        /// <summary>
        /// 创建任务之后Tes回调id
        /// </summary>
        [Column("tes_task_id")]
        public string TesTaskID { get; set; }

        /// <summary>
        /// wms发送任务之后传入的任务iD
        /// </summary>
        [Column("wms_task_id")]
        public string WmsTaskID { get; set; }

        /// <summary>
        /// wms发送任务之后传入的任务类型
        /// </summary>
        [Column("wms_task_type")]
        public string WmsTaskType { get; set; }

        /// <summary>
        /// 任务起始位置
        /// </summary>
        [Column("current_position")]
        public string CurrentPosition { get; set; }

        /// <summary>
        /// 当前任务状态
        /// </summary>
        [Column("statu")]
        public string Statu { get; set; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        [Column("creation_time")]
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// 任务完成时间
        /// </summary>
        [Column("completion_time")]
        public DateTime? CompletionTime { get; set; }

        /// <summary>
        /// 目标位置
        /// </summary>
        [Column("target_location")]
        public string TargetLocation { get; set; }

        /// <summary>
        /// 容器号
        /// </summary>
        [Column("pod_id")]
        public string Podid { get; set; }
    }
}