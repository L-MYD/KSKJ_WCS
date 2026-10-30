using WCS_Common.Enums;

namespace WCS_Common.Helper
{
    /// <summary>
    /// nan_T 2026-09-09：业务异常（参考 DTcms.Core.Common.Helper.ResponseException）。
    /// Service 层遇到业务规则不满足时（如用户名已存在、密码错误、数据不存在），
    /// 直接抛出本异常；由 GlobalExceptionFilter 统一捕获并转换为 ResponseMessage 响应，
    /// 避免在每个 Controller 方法中重复写 try/catch 和错误码判断。
    /// </summary>
    [Serializable]
    public class ResponseException : ApplicationException
    {
        /// <summary>nan_T 2026-09-09：映射到 HTTP 响应的状态码（如 400/401/403/404/500）</summary>
        private readonly int _code;

        /// <summary>nan_T 2026-09-09：业务错误码枚举</summary>
        private readonly ErrorCode _errorCode;

        /// <summary>
        /// nan_T 2026-09-09：默认构造，兜底提示“服务器繁忙，请稍后再试”
        /// </summary>
        public ResponseException() : base("服务器繁忙，请稍后再试!")
        {
            _errorCode = ErrorCode.Fail;
            _code = 400;
        }

        /// <summary>
        /// nan_T 2026-09-09：带消息的构造，可指定业务错误码与 HTTP 状态码
        /// </summary>
        /// <param name="message">面向前端的错误提示</param>
        /// <param name="errorCode">业务错误码（默认 Fail=400）</param>
        /// <param name="code">HTTP 状态码（默认 400）</param>
        public ResponseException(string? message = "服务器繁忙，请稍后再试!", ErrorCode errorCode = ErrorCode.Fail, int code = 400) : base(message)
        {
            _errorCode = errorCode;
            _code = code;
        }

        /// <summary>nan_T 2026-09-09：获取 HTTP 状态码</summary>
        public int GetCode() => _code;

        /// <summary>nan_T 2026-09-09：获取业务错误码</summary>
        public ErrorCode GetErrorCode() => _errorCode;
    }
}
