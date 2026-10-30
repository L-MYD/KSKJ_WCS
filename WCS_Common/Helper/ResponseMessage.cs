using WCS_Common.Enums;

namespace WCS_Common.Helper
{
    /// <summary>
    /// nan_T 2026-09-09：统一 API 响应消息体（参考 DTcms.Core.Common.Helper.ResponseMessage）。
    /// 所有 Controller 返回给前端的数据都包装为本结构，保证响应格式一致：
    /// { code, message, request }，成功/失败分别通过 Success/Error 工厂方法创建。
    /// 本类位于 Common 层，不依赖 ASP.NET Core，请求地址由上层过滤器统一赋值。
    /// </summary>
    public class ResponseMessage
    {
        /// <summary>业务错误码（取值与 HTTP 状态码对齐，200 表示成功）</summary>
        public ErrorCode Code { get; set; }

        /// <summary>提示消息或业务数据（失败时为错误提示，成功时可携带数据对象）</summary>
        public object? Message { get; set; }

        /// <summary>发生错误时的请求地址（方法 + 路径），便于日志定位；成功时可为空</summary>
        public string? Request { get; set; }

        /// <summary>
        /// nan_T 2026-09-09：无参构造，供序列化框架使用
        /// </summary>
        public ResponseMessage() { }

        /// <summary>
        /// nan_T 2026-09-09：仅指定错误码的构造
        /// </summary>
        public ResponseMessage(ErrorCode errorCode)
        {
            Code = errorCode;
        }

        /// <summary>
        /// nan_T 2026-09-09：指定错误码与消息的构造
        /// </summary>
        public ResponseMessage(ErrorCode errorCode, object? message)
        {
            Code = errorCode;
            Message = message;
        }

        /// <summary>
        /// nan_T 2026-09-09：构造成功响应，默认提示“操作成功”
        /// </summary>
        public static ResponseMessage Success(object? message = null)
        {
            return new ResponseMessage(ErrorCode.Success, message ?? "操作成功");
        }

        /// <summary>
        /// nan_T 2026-09-09：构造失败响应，默认提示“操作失败”
        /// </summary>
        public static ResponseMessage Error(string message = "操作失败")
        {
            return new ResponseMessage(ErrorCode.Fail, message);
        }
    }
}
