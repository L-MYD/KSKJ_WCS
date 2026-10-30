namespace WCS_IServices
{
    /// <summary>
    /// nan_T 2026-09-09：失败消息重发服务接口（PLC 指令 / API 请求 自动重发）。
    /// 由 WCS_Services 层的 RetryService 实现，RetryBackgroundService 定时调用
    /// <see cref="ProcessPendingAsync"/> 扫描 retry_messages 表中到期待发记录并重发。
    /// 继承 <see cref="IBaseService"/> 获得通用数据访问能力。
    /// </summary>
    public interface IRetryService : IBaseService
    {
        /// <summary>
        /// 扫描并处理一批到期待重发的消息（状态=待发送 且 下次重发时间已到）。
        /// API 消息按记录的 URL 重新 POST；PLC 消息通过 PLCService 注册的委托重新写入 PLC。
        /// 成功置为成功；失败按指数退避推迟下次重发；超过最大次数置为死信。
        /// </summary>
        /// <param name="batchSize">单次最多处理条数（默认 20，避免一次占用过久）</param>
        /// <returns>本次实际处理的记录条数</returns>
        Task<int> ProcessPendingAsync(int batchSize = 20);
    }
}
