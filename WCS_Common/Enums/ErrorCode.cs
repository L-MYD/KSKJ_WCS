namespace WCS_Common.Enums
{
    /// <summary>
    /// nan_T 2026-09-09：统一业务错误码枚举（参考 DTcms.Core.Common.Emum.ErrorCode）。
    /// 取值与 HTTP 状态码保持一致，便于全局异常过滤器直接映射为 HTTP 响应状态码。
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>成功</summary>
        Success = 200,

        /// <summary>失败（通用业务失败，如参数不合法、操作未满足前置条件）</summary>
        Fail = 400,

        /// <summary>未认证（未登录或凭证无效）</summary>
        Unauthorized = 401,

        /// <summary>无权限（已登录但角色/权限不足）</summary>
        Forbidden = 403,

        /// <summary>资源不存在</summary>
        NotFound = 404,

        /// <summary>模型校验失败（DTO 参数校验不通过）</summary>
        ParameterError = 422,

        /// <summary>服务器内部错误（未预期的异常）</summary>
        UnknownError = 500
    }
}
