using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WCS_Helper.Mapper;
using WCS_Models.LoginViewModel;
using WCS_Models.SqlModel;

using WCS_IServices;
namespace WCS_Services
{
    public class DataMonitoringService : IDataMonitoringService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DataMonitoringService> _logger;
        private readonly ILogService _logService;

        public DataMonitoringService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<DataMonitoringService> logger,
            ILogService logService)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _logService = logService;
        }

        public async Task<QueryModel<TescorrespondsWms>> GetTasklistAll(TaskQueryDto taskQueryDto)
        {
            var stopwatch = Stopwatch.StartNew();

            string clientIp = GetClientIp();
            string requestUrl = GetRequestUrl();

            try
            {
                _logService.AddWebLog("开始分页查询任务列表", "任务查询",
                    $"URL: {requestUrl}, 方法: GET, 页码: {taskQueryDto.page}, 页大小: {taskQueryDto.pageSize}, 客户端IP: {clientIp}",
                    "INFO", "Task", GetCurrentUserId());

                // 直接调用Mapper层的方法
                var result = await TaskDbContext.GetPagedTaskList(taskQueryDto);

                stopwatch.Stop();
                _logService.AddWebLog("分页任务列表查询完成", "任务查询",
                    $"页码: {taskQueryDto.page}, 页大小: {taskQueryDto.pageSize}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 总记录数: {result?.total ?? 0}",
                    "INFO", "Task", GetCurrentUserId());

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logService.AddWebLog("分页查询任务列表失败", "任务查询",
                    $"页码: {taskQueryDto.page}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 错误: {ex.Message}",
                    "ERROR", "Task", GetCurrentUserId());

                _logger.LogError(ex, "获取任务列表失败: {Message}", ex.Message);
                throw new Exception($"获取任务列表失败: {ex.Message}", ex);
            }
        }

        public async Task<QueryModel<TescorrespondsWms>> GetHistoryTasklistAll(TaskHistoryQueryParams taskHistoryQueryParams)
        {
            var stopwatch = Stopwatch.StartNew();

            string clientIp = GetClientIp();
            string requestUrl = GetRequestUrl();

            try
            {
                _logService.AddWebLog("开始分页查询历史任务列表", "历史任务查询",
                    $"URL: {requestUrl}, 方法: GET, 页码: {taskHistoryQueryParams.page}, 页大小: {taskHistoryQueryParams.pageSize}, 客户端IP: {clientIp}",
                    "INFO", "Task", GetCurrentUserId());

                // 直接调用Mapper层的方法
                var result = await TaskDbContext.GetHistoryTaskList(taskHistoryQueryParams);

                stopwatch.Stop();
                _logService.AddWebLog("历史任务列表查询完成", "历史任务查询",
                    $"页码: {taskHistoryQueryParams.page}, 页大小: {taskHistoryQueryParams.pageSize}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 总记录数: {result?.total ?? 0}",
                    "INFO", "Task", GetCurrentUserId());

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logService.AddWebLog("分页查询历史任务列表失败", "历史任务查询",
                    $"页码: {taskHistoryQueryParams.page}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 错误: {ex.Message}",
                    "ERROR", "Task", GetCurrentUserId());

                _logger.LogError(ex, "获取历史任务列表失败: {Message}", ex.Message);
                throw new Exception($"获取历史任务列表失败: {ex.Message}", ex);
            }
        }

        private string GetRequestUrl()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return "N/A";

                var request = httpContext.Request;

                // 构建完整的 URL
                var uriBuilder = new UriBuilder
                {
                    Scheme = request.Scheme,
                    Host = request.Host.Host,
                    Port = request.Host.Port ?? (request.Scheme == "https" ? 443 : 80),
                    Path = request.Path.ToString(),
                    Query = request.QueryString.ToString()
                };

                return uriBuilder.Uri.ToString();
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetClientIp()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext == null)
                    return "127.0.0.1";

                var request = httpContext.Request;

                string ip = request.Headers["X-Forwarded-For"].ToString() ??
                           request.Headers["X-Real-IP"].ToString() ??
                           httpContext.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrEmpty(ip) && ip.Contains(","))
                {
                    ip = ip.Split(',')[0].Trim();
                }

                if (string.IsNullOrEmpty(ip) || ip.ToLower() == "unknown" || ip == "::1")
                    ip = httpContext.Connection.RemoteIpAddress?.ToString();

                if (ip == "::1" || ip == "127.0.0.1")
                    ip = "127.0.0.1";

                return ip ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private string GetCurrentUserId()
        {
            try
            {
                if (_httpContextAccessor?.HttpContext == null)
                    return "system";

                var user = _httpContextAccessor.HttpContext.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.Claims.FirstOrDefault(c =>
                        c.Type == "UserId" ||
                        c.Type == "sub" ||
                        c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

                    if (userIdClaim != null)
                        return userIdClaim.Value;

                    return user.Identity.Name ?? "system";
                }

                var userIdFromHeader = _httpContextAccessor.HttpContext.Request.Headers["X-User-Id"].ToString();
                if (!string.IsNullOrEmpty(userIdFromHeader))
                    return userIdFromHeader;

                return "system";
            }
            catch
            {
                return "system";
            }
        }
    }
}