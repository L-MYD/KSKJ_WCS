using System.Collections.Generic;

namespace WCS_Api.Services
{
    /// <summary>
    /// nan_T 2026-11-12：日志/任务表自动备份清理配置（IOptions 注入，appsettings.json LogCleanup 节点）。
    /// 参考 GZNQ_WCS 的 LogCleanupOptions：每天定时将超期数据导出 CSV 备份后物理删除。
    /// </summary>
    public class LogCleanupOptions
    {
        /// <summary>logs 表保留天数。默认 30。设为 0 或负数表示不清理。</summary>
        public int LogRetentionDays { get; set; } = 30;

        /// <summary>tescorresponds_wms 任务表保留天数（仅清理已完成任务）。默认 30。设为 0 或负数表示不清理。</summary>
        public int TaskRetentionDays { get; set; } = 30;

        /// <summary>备份根目录（相对 ContentRootPath 或绝对路径）。默认 "Logs_Archive"。</summary>
        public string BackupRootDirectory { get; set; } = "Logs_Archive";

        /// <summary>每天执行时间（HH:mm，24 小时制）。默认 "02:00"。</summary>
        public string ExecutionTime { get; set; } = "02:00";

        /// <summary>
        /// 扫描的日志源目录列表（文件日志）。留空或未配置时使用内置默认值。
        /// </summary>
        public List<string> SourceDirectories { get; set; } = new List<string>();

        /// <summary>文件日志保留天数。默认 30。</summary>
        public int FileRetentionDays { get; set; } = 30;
    }
}
