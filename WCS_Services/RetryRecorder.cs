using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WCS_Helper;
using WCS_Models.WCSModel;
using WCS_Common.Helper;

namespace WCS_Services
{
    /// <summary>
    /// nan_T 2026-09-09：失败消息重发记录器（静态桥接，参考 CallbackCache 的用法）。
    /// 非 DI 托管的代码（ToTesApiService 的静态方法、手动 new 的 PLCService）无法直接注入
    /// IRetryService，统一通过本类把“发送失败的 API 请求 / PLC 指令”落库到 retry_messages 表，
    /// 由 RetryBackgroundService 后台定时扫描并自动重发；全程不依赖 txt 文件。
    /// PLC 的实际重发动作由 PLCService 启动时通过 <see cref="RegisterPlcSender"/> 注册委托实现。
    /// </summary>
    public static class RetryRecorder
    {
        /// <summary>
        /// nan_T 2026-09-09：PLC 重发委托列表。系统中存在多个 PLC 连接（每个 PLCService 实例对应一个 PLC IP），
        /// 每个实例启动时注册自己的发送器；发送器返回值约定：
        /// null = 该消息不属于本 PLC（plcip 不匹配）；true = 重发成功；false = 本 PLC 重发失败。
        /// </summary>
        private static readonly ConcurrentBag<Func<RetryMessage, Task<bool?>>> _plcSenders = new();

        /// <summary>nan_T 2026-09-09：注册 PLC 重发委托（每个 PLCService 实例构造时调用）</summary>
        public static void RegisterPlcSender(Func<RetryMessage, Task<bool?>> sender)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            _plcSenders.Add(sender);
            LogHelper.Plc("RetryRecorder", "已注册一个 PLC 重发委托");
        }

        /// <summary>nan_T 2026-09-09：获取全部已注册的 PLC 重发委托（重发服务逐个询问直到有实例认领）</summary>
        public static IReadOnlyCollection<Func<RetryMessage, Task<bool?>>> GetPlcSenders() => _plcSenders.ToArray();

        /// <summary>
        /// nan_T 2026-09-09：记录一条发送失败的 API 请求，等待后台自动重发（HTTP POST JSON）。
        /// </summary>
        /// <param name="url">完整请求地址</param>
        /// <param name="payloadJson">请求体 JSON 字符串</param>
        /// <param name="bizKey">业务主键（任务号等，便于排查）</param>
        /// <param name="msgType">业务类型（如 CreateMoveTask）</param>
        /// <param name="lastError">失败原因</param>
        public static void EnqueueApi(string url, string payloadJson, string bizKey, string msgType, string lastError)
        {
            Enqueue(new RetryMessage
            {
                Source = "API",
                MsgType = msgType ?? "API",
                TargetUrl = url,
                Payload = payloadJson,
                BizKey = bizKey,
                LastError = lastError,
                NextRetryTime = DateTimeHelper.CnNow.AddSeconds(30) // nan_T 2026-09-09：首次重发延迟 30 秒
            });
        }

        /// <summary>
        /// nan_T 2026-09-09：记录一条发送失败的 PLC 指令，等待后台自动重发（DBWrite）。
        /// </summary>
        /// <param name="payloadJson">指令元数据 JSON（DB 号/起始地址/HEX 字节/PLC 标识等）</param>
        /// <param name="bizKey">业务主键（箱号/站点等）</param>
        /// <param name="msgType">业务类型（如 DB4写、DB5回复）</param>
        /// <param name="lastError">失败原因</param>
        public static void EnqueuePlc(string payloadJson, string bizKey, string msgType, string lastError)
        {
            Enqueue(new RetryMessage
            {
                Source = "PLC",
                MsgType = msgType ?? "PLC",
                Payload = payloadJson,
                BizKey = bizKey,
                LastError = lastError,
                NextRetryTime = DateTimeHelper.CnNow.AddSeconds(15) // nan_T 2026-09-09：PLC 指令首次重发延迟 15 秒
            });
        }

        /// <summary>
        /// nan_T 2026-09-09：异步落库一条重发记录（后台线程执行，不阻塞主流程；
        /// 落库本身失败时仅记录日志，不再有任何 txt 兜底）。
        /// </summary>
        private static void Enqueue(RetryMessage message)
        {
            // nan_T 2026-09-09：后台线程入队，避免 PLC 循环 / API 调用被数据库 IO 阻塞
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using var context = new WcsDbContext();
                    context.RetryMessages.Add(message);
                    context.SaveChanges();

                    if (message.Source == "PLC")
                        LogHelper.Plc("重发记录", $"PLC 消息发送失败已入重发表，Id={message.Id}，类型={message.MsgType}，业务键={message.BizKey}", isError: true, details: message.LastError);
                    else
                        LogHelper.Api("重发记录", $"API 请求失败已入重发表，Id={message.Id}，地址={message.TargetUrl}，业务键={message.BizKey}", isError: true, details: message.LastError);
                }
                catch (Exception ex)
                {
                    // nan_T 2026-09-09：重发记录落库失败只打控制台/日志库（LogHelper 内部自带兜底），绝不写 txt
                    Console.WriteLine($"[RetryRecorder] 重发记录落库失败: {ex.Message}");
                }
            });
        }
    }
}
