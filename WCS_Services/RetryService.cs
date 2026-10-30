using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using WCS_Common.Enums;
using WCS_Helper;
using WCS_Helper.Database;
using WCS_IServices;
using WCS_Models.WCSModel;

namespace WCS_Services
{
    /// <summary>
    /// nan_T 2026-09-09：失败消息重发服务实现（参考 DTcms.Core.Services 分层写法）。
    /// 继承 <see cref="BaseService"/>，定时扫描 retry_messages 表：
    /// - API 消息：用 HttpClient 向记录的 TargetUrl 重新 POST Payload(JSON)；
    /// - PLC 消息：调用 <see cref="RetryRecorder"/> 中 PLCService 注册的委托重新写入 PLC；
    /// 重发成功置 Status=1；失败重试次数 +1 并按指数退避（30s→60s→120s…上限 30 分钟）推迟；
    /// 达到最大次数（默认 5）置 Status=2 死信并记录错误日志。全程仅写数据库，不产生 txt。
    /// </summary>
    public class RetryService : BaseService, IRetryService
    {
        /// <summary>nan_T 2026-09-09：复用静态 HttpClient，避免高频创建导致套接字耗尽</summary>
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>nan_T 2026-09-09：指数退避上限（30 分钟）</summary>
        private const int MaxBackoffSeconds = 30 * 60;

        private readonly ILogger<RetryService> _logger;

        /// <summary>
        /// 构造函数：注入数据库工厂与日志组件
        /// </summary>
        public RetryService(IDbContextFactory contextFactory, ILogger<RetryService> logger)
            : base(contextFactory)
        {
            _logger = logger;
        }

        /// <summary>
        /// 扫描并处理一批到期待重发的消息
        /// </summary>
        public async Task<int> ProcessPendingAsync(int batchSize = 20)
        {
            using var context = _contextFactory.CreateContext(WriteRoRead.Write);

            // nan_T 2026-09-09：取“待发送 且 到了下次重发时间”的记录，按入队先后处理
            var pending = await context.RetryMessages
                .Where(m => m.Status == 0 && m.NextRetryTime <= DateTime.UtcNow)
                .OrderBy(m => m.NextRetryTime)
                .Take(batchSize)
                .ToListAsync();

            if (pending.Count == 0)
            {
                return 0;
            }

            int processed = 0;
            foreach (var message in pending)
            {
                processed++;
                bool success;
                string error;

                try
                {
                    if (message.Source == "PLC")
                    {
                        (success, error) = await ResendPlcAsync(message);
                    }
                    else
                    {
                        (success, error) = await ResendApiAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    error = ex.Message;
                }

                message.RetryCount++;
                message.UpdateTime = DateTime.UtcNow;

                if (success)
                {
                    // 重发成功
                    message.Status = 1;
                    message.LastError = null;
                    LogResendSuccess(message);
                    _logger.LogInformation("重发成功：{Source} {MsgType} Id={Id} 业务键={BizKey}",
                        message.Source, message.MsgType, message.Id, message.BizKey);
                }
                else
                {
                    message.LastError = error;

                    if (message.RetryCount >= message.MaxRetry)
                    {
                        // 超过最大次数 → 死信，等待人工介入
                        message.Status = 2;
                        LogResendDead(message, error);
                        _logger.LogError("重发超过最大次数转为死信：{Source} {MsgType} Id={Id} 业务键={BizKey} 原因={Error}",
                            message.Source, message.MsgType, message.Id, message.BizKey, error);
                    }
                    else
                    {
                        // 指数退避：30s * 2^(已重试次数-1)，上限 30 分钟
                        int backoff = Math.Min(30 * (int)Math.Pow(2, message.RetryCount - 1), MaxBackoffSeconds);
                        message.NextRetryTime = DateTime.UtcNow.AddSeconds(backoff);
                        LogResendFail(message, error, backoff);
                        _logger.LogWarning("重发失败将在 {Backoff}s 后重试：{Source} {MsgType} Id={Id} 第{Count}次 原因={Error}",
                            backoff, message.Source, message.MsgType, message.Id, message.RetryCount, error);
                    }
                }
            }

            await context.SaveChangesAsync();
            return processed;
        }

        /// <summary>
        /// nan_T 2026-09-09：重发 API 请求——向 TargetUrl 重新 POST Payload(JSON)，HTTP 2xx 视为成功。
        /// </summary>
        private static async Task<(bool success, string error)> ResendApiAsync(RetryMessage message)
        {
            if (string.IsNullOrEmpty(message.TargetUrl))
            {
                return (false, "重发记录缺少目标地址 TargetUrl");
            }

            using var content = new StringContent(message.Payload ?? string.Empty, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(message.TargetUrl, content);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string body = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
        }

        /// <summary>
        /// nan_T 2026-09-09：重发 PLC 指令——逐个询问已注册的 PLCService 发送器（按 plcip 路由）：
        /// 返回 null 表示不属于该 PLC，继续询问下一个；返回 true/false 为认领后的重发结果。
        /// 无任何发送器注册或无实例认领时返回失败（消息保持待发，等 PLC 服务上线后再试）。
        /// </summary>
        private static async Task<(bool success, string error)> ResendPlcAsync(RetryMessage message)
        {
            var senders = RetryRecorder.GetPlcSenders();
            if (senders.Count == 0)
            {
                return (false, "PLC 重发委托未注册（PLC 服务可能尚未启动）");
            }

            bool anyClaimed = false;
            foreach (var sender in senders)
            {
                bool? result = await sender(message);
                if (result == null)
                {
                    continue; // nan_T 2026-09-09：该 plcip 不属于此发送器，继续路由
                }

                anyClaimed = true;
                return result.Value
                    ? (true, null)
                    : (false, "PLC 写入返回失败（连接未就绪或写入错误码非 0）");
            }

            return anyClaimed
                ? (false, "PLC 写入返回失败（连接未就绪或写入错误码非 0）")
                : (false, "没有找到匹配该消息 plcip 的 PLC 发送器（对应 PLC 服务可能未启动）");
        }

        /// <summary>nan_T 2026-09-09：重发成功日志（按来源分别记 API/PLC 日志）</summary>
        private static void LogResendSuccess(RetryMessage m)
        {
            string msg = $"重发成功 Id={m.Id} 类型={m.MsgType} 业务键={m.BizKey} 共重试{m.RetryCount}次";
            if (m.Source == "PLC") LogHelper.Plc("自动重发", msg);
            else LogHelper.Api("自动重发", msg);
        }

        /// <summary>nan_T 2026-09-09：重发失败（等待下次）日志</summary>
        private static void LogResendFail(RetryMessage m, string error, int backoffSeconds)
        {
            string msg = $"重发失败 Id={m.Id} 类型={m.MsgType} 业务键={m.BizKey} 第{m.RetryCount}次，{backoffSeconds}s 后再试";
            if (m.Source == "PLC") LogHelper.Plc("自动重发", msg, isError: true, details: error);
            else LogHelper.Api("自动重发", msg, isError: true, details: error);
        }

        /// <summary>nan_T 2026-09-09：死信日志（超过最大次数）</summary>
        private static void LogResendDead(RetryMessage m, string error)
        {
            string msg = $"重发超过最大次数({m.MaxRetry})转死信 Id={m.Id} 类型={m.MsgType} 业务键={m.BizKey}，需人工检查";
            if (m.Source == "PLC") LogHelper.Plc("自动重发-死信", msg, isError: true, details: error);
            else LogHelper.Api("自动重发-死信", msg, isError: true, details: error);
        }
    }
}
