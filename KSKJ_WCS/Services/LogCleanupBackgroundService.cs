using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WCS_Helper.Mapper;

namespace WCS_Api.Services
{
    /// <summary>
    /// nan_T 2026-11-12：日志/任务表自动备份+清理后台服务（参考 GZNQ_WCS LogCleanupBackgroundService）。
    ///   - 启动时立即执行一次；之后每天 LogCleanup:ExecutionTime（默认 02:00）执行
    ///   - logs 表：超 RetentionDays 的数据 → 导出 CSV 备份 → 物理删除
    ///   - tescorresponds_wms 表：仅清理已完成（completion_time 非空）且超期的任务 → 导出 CSV → 物理删除
    ///   - 文件日志：LastWriteTime 超期的文件 → 移到 Logs_Archive/yyyy-MM/ 子目录
    /// </summary>
    public class LogCleanupBackgroundService : BackgroundService
    {
        private readonly LogCleanupOptions _options;
        private readonly ILogger<LogCleanupBackgroundService> _logger;
        private readonly string _contentRootPath;

        public LogCleanupBackgroundService(
            IOptions<LogCleanupOptions> options,
            ILogger<LogCleanupBackgroundService> logger,
            IHostEnvironment hostEnvironment)
        {
            _options = options.Value;
            _logger = logger;
            _contentRootPath = hostEnvironment.ContentRootPath;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 启动即执行一次（早清理早生效）
            await SafeRunAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nextTime = ParseNextDailyRunTime(_options.ExecutionTime);
                    var delay = nextTime - DateTime.Now;
                    if (delay <= TimeSpan.Zero)
                        delay = delay.Add(TimeSpan.FromDays(1));

                    _logger.LogInformation("日志清理服务下次执行：{NextRun}（延迟 {Delay}）",
                        nextTime.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("日志清理服务被取消");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "计算下次执行时间异常，10 分钟后重试");
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }

                await SafeRunAsync(stoppingToken);
            }
        }

        private async Task SafeRunAsync(CancellationToken ct)
        {
            try
            {
                await RunAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志清理任务执行异常（已忽略，不影响主流程）");
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            var backupRoot = ResolveBackupRoot();
            var now = DateTime.Now;

            _logger.LogInformation("=== 开始日志清理：备份目录 {Backup}，当前时间 {Now} ===",
                backupRoot, now.ToString("yyyy-MM-dd HH:mm:ss"));

            // ---- 文件日志清理 ----
            await CleanupFileLogsAsync(ct, backupRoot, now);

            // ---- 数据库 logs 表清理 ----
            if (_options.LogRetentionDays > 0)
            {
                try
                {
                    var cutoff = now.AddDays(-_options.LogRetentionDays);
                    var file = Path.Combine(backupRoot, now.ToString("yyyy-MM"),
                        $"Logs_{now:yyyyMMdd_HHmmss}.csv");
                    int exported = LogsDbContext.ExportLogsBefore(cutoff, file);
                    if (exported > 0)
                    {
                        int deleted = LogsDbContext.DeleteLogsBefore(cutoff);
                        if (deleted == 0)
                            _logger.LogWarning("logs 表导出 {N} 条但删除 0 条（截止 {Cutoff}）", exported, cutoff);
                        else
                            _logger.LogInformation("logs 表清理完成：备份 {File}（{N} 条），删除 {D} 条", file, exported, deleted);
                    }
                    else
                    {
                        _logger.LogInformation("logs 表无超期数据（截止 {Cutoff}）", cutoff);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "logs 表清理失败");
                }
            }

            // ---- 数据库 tescorresponds_wms 任务表清理（仅已完成） ----
            if (_options.TaskRetentionDays > 0)
            {
                try
                {
                    var cutoff = now.AddDays(-_options.TaskRetentionDays);
                    var file = Path.Combine(backupRoot, now.ToString("yyyy-MM"),
                        $"Tasks_{now:yyyyMMdd_HHmmss}.csv");
                    int exported = TaskDbContext.ExportCompletedTasksBefore(cutoff, file);
                    if (exported > 0)
                    {
                        int deleted = TaskDbContext.DeleteCompletedTasksBefore(cutoff);
                        if (deleted == 0)
                            _logger.LogWarning("tescorresponds_wms 表导出 {N} 条但删除 0 条（截止 {Cutoff}）", exported, cutoff);
                        else
                            _logger.LogInformation("tescorresponds_wms 表清理完成：备份 {File}（{N} 条），删除 {D} 条", file, exported, deleted);
                    }
                    else
                    {
                        _logger.LogInformation("tescorresponds_wms 表无超期已完成数据（截止 {Cutoff}）", cutoff);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "tescorresponds_wms 表清理失败");
                }
            }
        }

        private Task CleanupFileLogsAsync(CancellationToken ct, string backupRoot, DateTime now)
        {
            if (_options.FileRetentionDays <= 0) return Task.CompletedTask;

            var cutoff = now.AddDays(-_options.FileRetentionDays);
            int totalBackedUp = 0, totalFailed = 0;

            foreach (var sourceDir in ResolveSourceDirectories())
            {
                if (ct.IsCancellationRequested) break;
                if (!Directory.Exists(sourceDir)) continue;

                try
                {
                    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                    {
                        if (ct.IsCancellationRequested) break;
                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.LastWriteTime >= cutoff) continue;

                            var yearMonth = fi.LastWriteTime.ToString("yyyy-MM");
                            var targetDir = Path.Combine(backupRoot, new DirectoryInfo(sourceDir).Name, yearMonth);
                            Directory.CreateDirectory(targetDir);

                            var targetPath = Path.Combine(targetDir, fi.Name);
                            if (File.Exists(targetPath))
                                targetPath = Path.Combine(targetDir,
                                    $"{Path.GetFileNameWithoutExtension(fi.Name)}_{fi.LastWriteTime:yyyyMMddHHmmss}{fi.Extension}");

                            try
                            {
                                fi.MoveTo(targetPath);
                            }
                            catch (IOException)
                            {
                                File.Copy(file, targetPath, overwrite: true);
                                File.Delete(file);
                            }
                            totalBackedUp++;
                        }
                        catch (Exception ex)
                        {
                            totalFailed++;
                            _logger.LogWarning(ex, "处理文件失败：{File}", file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "扫描目录失败：{Dir}", sourceDir);
                }
            }

            _logger.LogInformation("文件日志清理完成：备份 {N} 个，失败 {F} 个", totalBackedUp, totalFailed);
            return Task.CompletedTask;
        }

        private string ResolveBackupRoot()
        {
            var path = _options.BackupRootDirectory ?? "Logs_Archive";
            return Path.IsPathRooted(path) ? path : Path.Combine(_contentRootPath, path);
        }

        private string[] ResolveSourceDirectories()
        {
            if (_options.SourceDirectories != null && _options.SourceDirectories.Count > 0)
                return _options.SourceDirectories
                    .Select(p => Path.IsPathRooted(p) ? p : Path.Combine(_contentRootPath, p))
                    .Distinct().ToArray();

            return new[]
            {
                Path.Combine(_contentRootPath, "Logs"),
            }.Distinct().ToArray();
        }

        private static DateTime ParseNextDailyRunTime(string execTime)
        {
            var now = DateTime.Now;
            if (!TimeSpan.TryParse(execTime, out var ts))
                ts = new TimeSpan(2, 0, 0);
            var todayRun = now.Date.Add(ts);
            return todayRun > now ? todayRun : todayRun.AddDays(1);
        }
    }
}
