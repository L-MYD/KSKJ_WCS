using WCS_IServices;

namespace WCS_Api.Services
{
    /// <summary>
    /// nan_T 2026-09-09：失败消息自动重发后台服务（.NET 泛型主机托管服务 BackgroundService）。
    /// 每 15 秒扫描一次 retry_messages 表中到期待发的失败消息（PLC 指令 / API 请求），
    /// 调用 <see cref="IRetryService.ProcessPendingAsync"/> 执行重发；
    /// 单轮扫描出现异常时只记录日志、不退出后台循环，保证服务长期稳定运行。
    /// </summary>
    public class RetryBackgroundService : BackgroundService
    {
        /// <summary>nan_T 2026-09-09：扫描间隔（15 秒）</summary>
        private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);

        /// <summary>nan_T 2026-09-09：单轮最多处理条数</summary>
        private const int BatchSize = 20;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RetryBackgroundService> _logger;

        /// <summary>
        /// 构造函数：注入 DI 作用域工厂（后台服务本身是单例，需手动创建作用域解析 Scoped 服务）
        /// </summary>
        public RetryBackgroundService(IServiceScopeFactory scopeFactory, ILogger<RetryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// 后台主循环：定时扫描并重发
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("失败消息自动重发服务已启动，扫描间隔 {Interval}s", ScanInterval.TotalSeconds);

            // nan_T 2026-09-09：启动后先延迟一个间隔再开始，避免应用尚未完全就绪时抢跑
            try
            {
                await Task.Delay(ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var retryService = scope.ServiceProvider.GetRequiredService<IRetryService>();
                    int processed = await retryService.ProcessPendingAsync(BatchSize);

                    if (processed > 0)
                    {
                        _logger.LogInformation("本轮重发处理 {Count} 条失败消息", processed);
                    }
                }
                catch (Exception ex)
                {
                    // nan_T 2026-09-09：单轮异常不能杀死后台循环，记录后下一轮继续
                    _logger.LogError(ex, "自动重发扫描轮次异常: {Message}", ex.Message);
                }

                try
                {
                    await Task.Delay(ScanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("失败消息自动重发服务已停止");
        }
    }
}
