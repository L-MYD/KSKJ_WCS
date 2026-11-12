using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using WCS_Common.Enums;
using WCS_Common.Helper;
using WCS_Helper.Database;
using WCS_IServices;
using WCS_Models.LoginViewModel;
using WCS_Models.SqlModel;

namespace WCS_Services
{
    /// <summary>
    /// nan_T 2026-09-09：数据监控服务实现（参考 DTcms.Core.Services 的分层写法）。
    /// 继承 <see cref="BaseService"/>，通过 <see cref="IDbContextFactory"/> 创建只读上下文完成分页查询；
    /// 原静态 TaskDbContext.GetPagedTaskList / GetHistoryTaskList 中的查询逻辑内聚到本服务，
    /// 查询条件组装、排序、分页均使用 EF Core 异步方法；业务失败抛出 <see cref="ResponseException"/>
    /// 由全局异常过滤器统一转换响应。操作审计日志仍通过 <see cref="ILogService"/> 记录。
    /// </summary>
    public class DataMonitoringService : BaseService, IDataMonitoringService
    {
        /// <summary>nan_T 2026-09-09：HTTP 上下文访问器，用于获取客户端 IP、请求地址、当前登录用户</summary>
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>nan_T 2026-09-09：日志记录器</summary>
        private readonly ILogger<DataMonitoringService> _logger;

        /// <summary>nan_T 2026-09-09：Web 操作审计日志服务（落库记录查询行为）</summary>
        private readonly ILogService _logService;

        /// <summary>
        /// 构造函数：注入数据库工厂、HTTP 上下文、日志与审计服务
        /// </summary>
        public DataMonitoringService(
            IDbContextFactory contextFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DataMonitoringService> logger,
            ILogService logService)
            : base(contextFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _logService = logService;
        }

        /// <summary>
        /// 分页查询当前任务列表（默认仅查询当天任务）。
        /// 支持：日期范围、状态、任务类型、关键词、容器号、位置、创建/完成时间范围、超时任务过滤、动态排序。
        /// </summary>
        /// <param name="taskQueryDto">查询条件（页码、页大小及各类筛选条件）</param>
        /// <returns>分页结果</returns>
        /// <exception cref="ResponseException">查询发生异常时抛出（500）</exception>
        public async Task<QueryModel<TescorrespondsWms>> GetTasklistAll(TaskQueryDto taskQueryDto)
        {
            var stopwatch = Stopwatch.StartNew();
            string clientIp = GetClientIp();
            string requestUrl = GetRequestUrl();

            _logService.AddWebLog("开始分页查询任务列表", "任务查询",
                $"URL: {requestUrl}, 方法: GET, 页码: {taskQueryDto.page}, 页大小: {taskQueryDto.pageSize}, 客户端IP: {clientIp}",
                "INFO", "Task", GetCurrentUserId());

            try
            {
                // nan_T 2026-09-09：查询走只读上下文（using 保证释放），逻辑由原静态 TaskDbContext 迁移而来
                using var context = _contextFactory.CreateContext(WriteRoRead.Read);
                var query = context.TescorrespondsWms.AsQueryable();

                // 1. 日期范围：传入起止日期按范围过滤；未传时默认只查当天
                if (!string.IsNullOrEmpty(taskQueryDto.startDate) || !string.IsNullOrEmpty(taskQueryDto.endDate))
                {
                    DateTime? startDate = DateTime.TryParse(taskQueryDto.startDate, out var parsedStart) ? parsedStart : null;
                    DateTime? endDate = DateTime.TryParse(taskQueryDto.endDate, out var parsedEnd)
                        ? parsedEnd.Date.AddDays(1).AddSeconds(-1) // nan_T 2026-09-09：结束日期取当天最后一秒
                        : null;

                    if (startDate.HasValue && endDate.HasValue)
                        query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
                    else if (startDate.HasValue)
                        query = query.Where(t => t.CreationTime >= startDate);
                    else if (endDate.HasValue)
                        query = query.Where(t => t.CreationTime <= endDate);
                }
                else
                {
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    query = query.Where(t => t.CreationTime >= today && t.CreationTime < tomorrow);
                }

                // 2. 公共字符串类筛选（状态/类型/关键词/容器号/位置）
                query = ApplyCommonFilters(query, taskQueryDto.status, taskQueryDto.type,
                    taskQueryDto.keyword, taskQueryDto.podId, taskQueryDto.location);

                // 3. 高级搜索 - 创建时间范围（DateTime? 类型）
                if (taskQueryDto.creationStartTime.HasValue && taskQueryDto.creationEndTime.HasValue)
                {
                    var start = taskQueryDto.creationStartTime.Value;
                    var end = taskQueryDto.creationEndTime.Value.Date.AddDays(1).AddTicks(-1); // 当天最后一刻
                    query = query.Where(t => t.CreationTime >= start && t.CreationTime <= end);
                }

                // 4. 高级搜索 - 完成时间范围（DateTime? 类型）
                if (taskQueryDto.completionStartTime.HasValue && taskQueryDto.completionEndTime.HasValue)
                {
                    var start = taskQueryDto.completionStartTime.Value;
                    var end = taskQueryDto.completionEndTime.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => t.CompletionTime.HasValue
                                           && t.CompletionTime.Value >= start
                                           && t.CompletionTime.Value <= end);
                }
                // nan_T 2026-09-09：执行时长范围（minDuration/maxDuration）原逻辑依赖 SQL DATEDIFF，
                // EF Core 无法直接翻译，保持原实现暂不处理，如需精确可改用原始 SQL。

                // 5. 异常类型过滤："超时任务" 为特殊规则（创建超过 30 分钟且未完成），其余按状态精确匹配
                if (!string.IsNullOrEmpty(taskQueryDto.exceptionType))
                {
                    if (taskQueryDto.exceptionType == "超时任务")
                    {
                        var cutoffTime = DateTimeHelper.CnNow.AddMinutes(-30);
                        var completedStatuses = new[] { "到达目的地", "拆膜完成", "异常处理完成" };
                        query = query.Where(t => !completedStatuses.Contains(t.Statu)
                                                 && t.CreationTime < cutoffTime);
                    }
                    else
                    {
                        query = query.Where(t => t.Statu == taskQueryDto.exceptionType);
                    }
                }

                // 6. 动态排序（默认按创建时间倒序）
                query = ApplySorting(query, taskQueryDto.sortBy, taskQueryDto.sortOrder);

                // 7. 先取总数再分页
                var totalCount = await query.CountAsync();
                var data = await query
                    .Skip((taskQueryDto.page - 1) * taskQueryDto.pageSize)
                    .Take(taskQueryDto.pageSize)
                    .ToListAsync();

                stopwatch.Stop();
                _logService.AddWebLog("分页任务列表查询完成", "任务查询",
                    $"页码: {taskQueryDto.page}, 页大小: {taskQueryDto.pageSize}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 总记录数: {totalCount}",
                    "INFO", "Task", GetCurrentUserId());

                return new QueryModel<TescorrespondsWms>
                {
                    list = data,
                    total = totalCount,
                    page = taskQueryDto.page,
                    pageSize = taskQueryDto.pageSize
                };
            }
            catch (ResponseException)
            {
                throw; // nan_T 2026-09-09：业务异常直接上抛，交由全局过滤器处理
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logService.AddWebLog("分页查询任务列表失败", "任务查询",
                    $"页码: {taskQueryDto.page}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 错误: {ex.Message}",
                    "ERROR", "Task", GetCurrentUserId());
                _logger.LogError(ex, "获取任务列表失败: {Message}", ex.Message);

                // nan_T 2026-09-09：统一抛业务异常，由 GlobalExceptionFilter 转换为 500 响应
                throw new ResponseException($"获取任务列表失败: {ex.Message}", ErrorCode.UnknownError, 500);
            }
        }

        /// <summary>
        /// 分页查询历史任务列表（默认查询最近 7 天任务）。
        /// 支持：日期范围、状态、任务类型、关键词、容器号、位置、创建/完成时间范围、动态排序。
        /// </summary>
        /// <param name="queryParams">历史任务查询条件</param>
        /// <returns>分页结果</returns>
        /// <exception cref="ResponseException">查询发生异常时抛出（500）</exception>
        public async Task<QueryModel<TescorrespondsWms>> GetHistoryTasklistAll(TaskHistoryQueryParams queryParams)
        {
            var stopwatch = Stopwatch.StartNew();
            string clientIp = GetClientIp();
            string requestUrl = GetRequestUrl();

            _logService.AddWebLog("开始分页查询历史任务列表", "历史任务查询",
                $"URL: {requestUrl}, 方法: GET, 页码: {queryParams.page}, 页大小: {queryParams.pageSize}, 客户端IP: {clientIp}",
                "INFO", "Task", GetCurrentUserId());

            try
            {
                // nan_T 2026-09-09：查询走只读上下文，逻辑由原静态 TaskDbContext.GetHistoryTaskList 迁移而来
                using var context = _contextFactory.CreateContext(WriteRoRead.Read);
                var query = context.TescorrespondsWms.AsQueryable();

                // 1. 日期范围：传入起止日期按范围过滤；未传时默认查最近 7 天
                if (!string.IsNullOrEmpty(queryParams.startDate) || !string.IsNullOrEmpty(queryParams.endDate))
                {
                    DateTime? startDate = DateTime.TryParse(queryParams.startDate, out var parsedStart) ? parsedStart : null;
                    DateTime? endDate = DateTime.TryParse(queryParams.endDate, out var parsedEnd)
                        ? parsedEnd.Date.AddDays(1).AddSeconds(-1)
                        : null;

                    if (startDate.HasValue && endDate.HasValue)
                        query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
                    else if (startDate.HasValue)
                        query = query.Where(t => t.CreationTime >= startDate);
                    else if (endDate.HasValue)
                        query = query.Where(t => t.CreationTime <= endDate);
                }
                else
                {
                    var endDate = DateTime.Today;
                    var startDate = endDate.AddDays(-7); // nan_T 2026-09-09：历史列表默认回溯 7 天
                    query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
                }

                // 2. 公共字符串类筛选（状态/类型/关键词/容器号/位置）
                query = ApplyCommonFilters(query, queryParams.status, queryParams.type,
                    queryParams.keyword, queryParams.podId, queryParams.location);

                // 3. 创建时间范围（TaskHistoryQueryParams 中为非空 DateTime，用 default 判断是否传入）
                if (queryParams.creationStartTime != default(DateTime) && queryParams.creationEndTime != default(DateTime))
                {
                    var cs = queryParams.creationStartTime;
                    var ce = queryParams.creationEndTime;
                    query = query.Where(t => t.CreationTime >= cs && t.CreationTime <= ce.AddDays(1).AddSeconds(-1));
                }

                // 4. 完成时间范围
                if (queryParams.completionStartTime != default(DateTime) && queryParams.completionEndTime != default(DateTime))
                {
                    var cps = queryParams.completionStartTime;
                    var cpe = queryParams.completionEndTime;
                    query = query.Where(t => t.CompletionTime >= cps && t.CompletionTime <= cpe.AddDays(1).AddSeconds(-1));
                }

                // 5. 动态排序（默认按创建时间倒序）
                query = ApplySorting(query, queryParams.sortBy, queryParams.sortOrder);

                // 6. 先取总数再分页
                var totalCount = await query.CountAsync();
                var data = await query
                    .Skip((queryParams.page - 1) * queryParams.pageSize)
                    .Take(queryParams.pageSize)
                    .ToListAsync();

                stopwatch.Stop();
                _logService.AddWebLog("历史任务列表查询完成", "历史任务查询",
                    $"页码: {queryParams.page}, 页大小: {queryParams.pageSize}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 总记录数: {totalCount}",
                    "INFO", "Task", GetCurrentUserId());

                return new QueryModel<TescorrespondsWms>
                {
                    list = data,
                    total = totalCount,
                    page = queryParams.page,
                    pageSize = queryParams.pageSize
                };
            }
            catch (ResponseException)
            {
                throw; // nan_T 2026-09-09：业务异常直接上抛
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logService.AddWebLog("分页查询历史任务列表失败", "历史任务查询",
                    $"页码: {queryParams.page}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 错误: {ex.Message}",
                    "ERROR", "Task", GetCurrentUserId());
                _logger.LogError(ex, "获取历史任务列表失败: {Message}", ex.Message);

                throw new ResponseException($"获取历史任务列表失败: {ex.Message}", ErrorCode.UnknownError, 500);
            }
        }

        /// <summary>
        /// nan_T 2026-09-09：应用任务列表的公共字符串筛选条件（当前任务与历史任务逻辑一致，抽取复用）。
        /// </summary>
        /// <param name="query">待筛选的查询源</param>
        /// <param name="status">任务状态</param>
        /// <param name="type">任务类型</param>
        /// <param name="keyword">关键词（匹配 WMS/TES 任务号）</param>
        /// <param name="podId">容器号（模糊匹配）</param>
        /// <param name="location">位置（匹配当前位置或目标位置）</param>
        private static IQueryable<TescorrespondsWms> ApplyCommonFilters(
            IQueryable<TescorrespondsWms> query,
            string? status, string? type, string? keyword, string? podId, string? location)
        {
            // 状态过滤
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Statu == status);
            }

            // 任务类型过滤
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(t => t.WmsTaskType == type);
            }

            // 关键词搜索（WMS 任务号 或 TES 任务号 模糊匹配）
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(t => t.WmsTaskID.Contains(keyword)
                                         || t.TesTaskID.Contains(keyword));
            }

            // 容器号搜索
            if (!string.IsNullOrEmpty(podId))
            {
                query = query.Where(t => t.Podid.Contains(podId));
            }

            // 位置搜索（当前位置 或 目标位置 模糊匹配）
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(t => t.CurrentPosition.Contains(location)
                                         || t.TargetLocation.Contains(location));
            }

            return query;
        }

        /// <summary>
        /// nan_T 2026-09-09：应用动态排序。sortOrder 为 "ascending" 时升序，其余（含默认）按倒序；
        /// 支持按创建时间/完成时间/WMS任务号/TES任务号排序；未指定排序字段时默认按创建时间倒序。
        /// </summary>
        private static IQueryable<TescorrespondsWms> ApplySorting(
            IQueryable<TescorrespondsWms> query, string? sortBy, string? sortOrder)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return query.OrderByDescending(t => t.CreationTime);
            }

            bool ascending = string.Equals(sortOrder, "ascending", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                "CreationTime" => ascending
                    ? query.OrderBy(t => t.CreationTime)
                    : query.OrderByDescending(t => t.CreationTime),
                "CompletionTime" => ascending
                    ? query.OrderBy(t => t.CompletionTime)
                    : query.OrderByDescending(t => t.CompletionTime),
                "WmsTaskID" => ascending
                    ? query.OrderBy(t => t.WmsTaskID)
                    : query.OrderByDescending(t => t.WmsTaskID),
                "TesTaskID" => ascending
                    ? query.OrderBy(t => t.TesTaskID)
                    : query.OrderByDescending(t => t.TesTaskID),
                _ => query.OrderByDescending(t => t.CreationTime)
            };
        }

        /// <summary>
        /// 获取当前请求的完整 URL（用于审计日志）；获取失败返回 "N/A"。
        /// </summary>
        private string GetRequestUrl()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return "N/A";

                var request = httpContext.Request;

                // 拼接 协议+主机+端口+路径+查询串
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

        /// <summary>
        /// 获取客户端真实 IP（优先取反向代理转发头 X-Forwarded-For / X-Real-IP）；
        /// 获取失败或本机回环地址统一返回 "127.0.0.1"。
        /// </summary>
        private string GetClientIp()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext == null)
                    return "127.0.0.1";

                var request = httpContext.Request;

                // 反向代理场景下 X-Forwarded-For 可能包含多个 IP（逗号分隔），取第一个
                // nan_T 2026-09-09：RemoteIpAddress?.ToString() 可能为 null，故 ip 声明为可空
                string? ip = request.Headers["X-Forwarded-For"].ToString() ??
                           request.Headers["X-Real-IP"].ToString() ??
                           httpContext.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrEmpty(ip) && ip.Contains(","))
                {
                    ip = ip.Split(',')[0].Trim();
                }

                // 未知或 IPv6 回环地址回退到连接远程地址
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

        /// <summary>
        /// 获取当前操作用户标识（用于审计日志）：
        /// 已登录取声明中的用户 Id，未登录尝试取请求头 X-User-Id，均无则返回 "system"。
        /// </summary>
        private string GetCurrentUserId()
        {
            try
            {
                if (_httpContextAccessor?.HttpContext == null)
                    return "system";

                var user = _httpContextAccessor.HttpContext.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    // 依次匹配自定义 UserId、JWT 的 sub、.NET 标准 NameIdentifier 声明
                    var userIdClaim = user.Claims.FirstOrDefault(c =>
                        c.Type == "UserId" ||
                        c.Type == "sub" ||
                        c.Type == ClaimTypes.NameIdentifier);

                    if (userIdClaim != null)
                        return userIdClaim.Value;

                    return user.Identity.Name ?? "system";
                }

                // 未登录时允许通过请求头透传调用方标识
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
