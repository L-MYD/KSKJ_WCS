namespace WCS_Models.WCSModel
{
    /// <summary>
    /// nan_T 2026-09-09：PLC 重发指令报文模型（retry_messages 表中 Source=PLC 记录的 Payload 反序列化结构）。
    /// DB4/DB5 等写入失败时，PLCService 把指令元数据按本结构序列化为 JSON 落库；
    /// 后台重发服务再按本结构还原字节并重新执行 DBWrite。
    /// </summary>
    public class PlcRetryPayload
    {
        /// <summary>目标 PLC 的 IP（重发时按此字段路由到对应的 PLCService 实例）</summary>
        public string Plcip { get; set; }

        /// <summary>PLC 标识（如输送线编号，用于日志区分）</summary>
        public string Symbol { get; set; }

        /// <summary>DB 块号（如 DB4 对应 4）</summary>
        public int Db { get; set; }

        /// <summary>写入起始地址（字节偏移）</summary>
        public int Start { get; set; }

        /// <summary>写入长度（字节）</summary>
        public int Length { get; set; }

        /// <summary>
        /// 指令字节的 HEX 字符串（BitConverter.ToString 格式，如 "01-0A-FF"），
        /// 重发时按 "-" 分割并逐字节还原为 byte[]。
        /// </summary>
        public string Hex { get; set; }
    }
}
