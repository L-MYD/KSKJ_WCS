using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WCS_Common.Enums;
using WCS_Common.Helper;

namespace WCS_Api.Filters
{
    /// <summary>
    /// nan_T 2026-09-09：全局异常过滤器（参考 DTcms.Core.API.Filters.GlobalExceptionFilter）。
    /// 统一拦截 Controller/Service 层未处理的异常并转换为标准 <see cref="ResponseMessage"/> 响应：
    /// 1) 业务异常 <see cref="ResponseException"/>：按其携带的错误码与 HTTP 状态码返回，记录警告日志；
    /// 2) 其他未预期异常：返回 500，开发环境返回详细错误信息，生产环境只返回友好提示。
    /// 这样 Service 层可以直接 throw 业务异常，Controller 无需再逐个 try/catch。
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        /// <summary>nan_T 2026-09-09：日志记录器</summary>
        private readonly ILogger<GlobalExceptionFilter> _logger;

        /// <summary>nan_T 2026-09-09：宿主环境信息，用于区分开发/生产环境的错误详情输出</summary>
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// 构造函数：由 MVC 过滤器管道自动注入
        /// </summary>
        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// 异常发生时的统一处理入口
        /// </summary>
        public void OnException(ExceptionContext context)
        {
            // 1. 业务异常：按异常中携带的业务码/HTTP状态码响应
            if (context.Exception is ResponseException bizException)
            {
                var warnResponse = new ResponseMessage(bizException.GetErrorCode(), bizException.Message)
                {
                    Request = context.HttpContext.Request.Method + " " + context.HttpContext.Request.Path
                };
                _logger.LogWarning("业务异常：{Message} | 请求：{Request}",
                    bizException.Message, warnResponse.Request);
                HandlerException(context, warnResponse, bizException.GetCode());
                return;
            }

            // 2. 未预期异常：递归收集完整异常链（含内部异常）信息
            string error = "异常信息：";
            void ReadException(Exception ex)
            {
                error += $"{ex.Message} | {ex.StackTrace} | {ex.InnerException}";
                if (ex.InnerException != null)
                {
                    ReadException(ex.InnerException);
                }
            }
            ReadException(context.Exception);
            _logger.LogError(context.Exception, "未处理异常：{Error}", error);

            // nan_T 2026-09-09：开发环境返回详细错误便于排查，生产环境隐藏堆栈等敏感信息
            object message = _environment.IsDevelopment() ? error : "服务器正忙，请稍后再试.";
            var apiResponse = new ResponseMessage(ErrorCode.UnknownError, message)
            {
                Request = context.HttpContext.Request.Method + " " + context.HttpContext.Request.Path
            };
            HandlerException(context, apiResponse, StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// nan_T 2026-09-09：把异常结果写入 MVC 上下文，标记异常已处理，避免框架再次抛出。
        /// </summary>
        /// <param name="context">异常上下文</param>
        /// <param name="apiResponse">统一响应消息体</param>
        /// <param name="statusCode">HTTP 状态码</param>
        private static void HandlerException(ExceptionContext context, ResponseMessage apiResponse, int statusCode)
        {
            context.Result = new JsonResult(apiResponse)
            {
                StatusCode = statusCode,
                ContentType = "application/json"
            };
            context.ExceptionHandled = true;
        }
    }
}
