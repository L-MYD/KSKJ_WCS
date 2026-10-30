using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WCS_IServices;
using WCS_Models.LoginViewModel;
using WCS_Models.SqlModel;

namespace WCS_Api.Controllers
{
    /// <summary>
    /// nan_T 2026-09-09：数据监控控制器（参考 DTcms.Core 控制器的瘦身写法）。
    /// 控制器只负责接收查询参数、调用服务、返回分页数据；
    /// 查询过程中的异常由 Service 抛出 <see cref="WCS_Common.Helper.ResponseException"/>，
    /// 经 GlobalExceptionFilter 统一转换为错误响应。
    /// 查询类接口成功时直接返回分页数据（与 DTcms 列表接口风格一致）。
    /// </summary>
    //[ApiController]
    [Route("api/Data")]
    public class DataMonitoringController : ControllerBase
    {
        /// <summary>nan_T 2026-09-09：数据监控服务（面向接口调用）</summary>
        private readonly IDataMonitoringService _dataMonitoringService;

        /// <summary>
        /// 构造函数：依赖注入数据监控服务
        /// </summary>
        public DataMonitoringController(IDataMonitoringService dataMonitoringService)
        {
            _dataMonitoringService = dataMonitoringService;
        }

        /// <summary>
        /// 分页获取当前任务列表（默认查询当天任务）。
        /// 示例：GET /api/Data/task?page=1&pageSize=20&status=到达目的地&keyword=WMS001
        /// </summary>
        /// <param name="taskQueryDto">查询条件（页码、页大小、日期范围、状态、类型、关键词、容器号、位置、排序等）</param>
        /// <returns>分页任务数据：list 当前页数据、total 总数、page/pageSize 分页信息</returns>
        [HttpGet("task")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTasklistAll([FromQuery] TaskQueryDto taskQueryDto)
        {
            // nan_T 2026-09-09：分页参数兜底，防止非法页码导致 Skip 负数
            NormalizePaging(taskQueryDto);
            var result = await _dataMonitoringService.GetTasklistAll(taskQueryDto);
            return Ok(result);
        }

        /// <summary>
        /// 分页获取历史任务列表（默认查询最近 7 天任务）。
        /// 示例：GET /api/Data/task/history?page=1&amp;pageSize=20&amp;startDate=2026-09-01&amp;endDate=2026-09-09
        /// </summary>
        /// <param name="taskHistoryQueryParams">历史任务查询条件（页码、页大小、日期范围、状态、类型、关键词等）</param>
        /// <returns>分页任务数据：list 当前页数据、total 总数、page/pageSize 分页信息</returns>
        [HttpGet("task/history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHistoryTasklistAll([FromQuery] TaskHistoryQueryParams taskHistoryQueryParams)
        {
            // nan_T 2026-09-09：分页参数兜底，防止非法页码导致 Skip 负数
            if (taskHistoryQueryParams.page < 1) taskHistoryQueryParams.page = 1;
            if (taskHistoryQueryParams.pageSize < 1) taskHistoryQueryParams.pageSize = 20;

            var result = await _dataMonitoringService.GetHistoryTasklistAll(taskHistoryQueryParams);
            return Ok(result);
        }

        /// <summary>
        /// nan_T 2026-09-09：规范化当前任务查询的分页参数（页码最小 1，每页条数最小 1、最大 200 防止一次拉取过多）。
        /// </summary>
        private static void NormalizePaging(TaskQueryDto dto)
        {
            if (dto.page < 1) dto.page = 1;
            if (dto.pageSize < 1) dto.pageSize = 20;
            if (dto.pageSize > 200) dto.pageSize = 200;
        }
    }
}
