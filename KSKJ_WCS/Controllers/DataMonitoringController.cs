using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using WCS_Models;
using WCS_Services;
using WCS_IServices;
using WCS_Models.SqlModel;
using WCS_Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WCS_Models.LoginViewModel;

using System.ComponentModel.DataAnnotations;

namespace WCS_Api.Controllers
{
    //[ApiController]
    [Route("api/Data")]
    public class DataMonitoringController : ControllerBase
    {
        private readonly IDataMonitoringService _dataMonitoringService;

        public DataMonitoringController(IDataMonitoringService dataMonitoringService)
        {
            _dataMonitoringService = dataMonitoringService;
        }

        [HttpGet("task")]
        public async Task<IActionResult> GetTasklistAll([FromQuery] TaskQueryDto taskQueryDto)
        {
            var result = await _dataMonitoringService.GetTasklistAll(taskQueryDto);
            return Ok(result);
        }

        [HttpGet("task/history")]
        public async Task<IActionResult> GetHistoryTasklistAll([FromQuery] TaskHistoryQueryParams taskHistoryQueryParams)
        {
            var result = await _dataMonitoringService.GetHistoryTasklistAll(taskHistoryQueryParams);
            return Ok(result);
        }
    }
}
