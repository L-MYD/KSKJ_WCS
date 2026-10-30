using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WCS_Models.WCSModel
{
    /// <summary>
    /// nan_T 2026-09-09：失败消息重发表实体（PLC 指令 / API 请求 统一落库，由后台服务自动重发）。
    /// 表名：retry_messages。所有外发失败的消息（PLC 写 DB 指令、HTTP 接口请求）
    /// 都先写入本表，RetryBackgroundService 按指数退避策略定时扫描重发，
    /// 成功后置为成功，超过最大次数置为死信，全程不再依赖 txt 文件。
    /// </summary>
    [Table("retry_messages")]
    public class RetryMessage
    {
        /// <summary>主键自增 Id</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>消息来源：PLC=PLC 指令；API=HTTP 接口请求（TES/WMS）</summary>
        [Column("source")]
        public string Source { get; set; } = "API";

        /// <summary>业务类型（如 CreateMoveTask、DB4写、DB5回复 等），便于分类排查</summary>
        [Column("msgtype")]
        public string? MsgType { get; set; }

        /// <summary>API 重发目标完整地址（PLC 消息为 null）</summary>
        [Column("targeturl")]
        public string? TargetUrl { get; set; }

        /// <summary>消息内容：API 为请求 JSON；PLC 为指令元数据 JSON（DB 号/起始地址/HEX 字节等）</summary>
        [Column("payload")]
        public string? Payload { get; set; }

        /// <summary>业务主键（任务号/站点/箱号等），用于日志关联与去重</summary>
        [Column("bizkey")]
        public string? BizKey { get; set; }

        /// <summary>状态：0=待发送（重发中），1=发送成功，2=超过最大次数（死信）</summary>
        [Column("status")]
        public int Status { get; set; } = 0;

        /// <summary>已重发次数</summary>
        [Column("retrycount")]
        public int RetryCount { get; set; } = 0;

        /// <summary>最大重发次数（默认 5 次，超过置为死信）</summary>
        [Column("maxretry")]
        public int MaxRetry { get; set; } = 5;

        /// <summary>下次允许重发的时间（指数退避后计算）</summary>
        [Column("nextretrytime")]
        public DateTime NextRetryTime { get; set; } = DateTime.UtcNow;

        /// <summary>最近一次失败原因（重发成功后置为 null）</summary>
        [Column("lasterror")]
        public string? LastError { get; set; }

        /// <summary>首次入库时间</summary>
        [Column("createtime")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>最近一次更新时间</summary>
        [Column("updatetime")]
        public DateTime? UpdateTime { get; set; }
    }
}
