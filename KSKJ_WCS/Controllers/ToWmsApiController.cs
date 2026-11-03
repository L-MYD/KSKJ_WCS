using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WCS_Helper;
using WCS_Helper.Mapper;
using WCS_Models;
using WCS_Models.SqlModel;
using WCS_Models.TESModel;
using WCS_Models.WCSModel;
using WCS_Models.WMSModel;
using WCS_Services;

namespace WCS_Api.Controllers
{
    [Route("[controller]")]
    public class ToWmsApiController : Controller
    {
        ToTesApiService toTesApiService = new ToTesApiService();

        /// <summary>
        /// 取消任务
        /// </summary>
        [HttpPost("CancelTask")]
        public async Task<IActionResult> CancelTask([FromBody] List<CancelTaskToWMS> tasklist)
        {
            try
            {
                if (tasklist == null || !tasklist.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空或任务列表为空"));
                }
                foreach (var jsonInput in tasklist)
                {
                    if (jsonInput == null)
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                    }
                }
                var results = new List<object>();
                foreach (var canceltask in tasklist)
                {
                    CancelTask testaskID = new CancelTask();
                    testaskID.taskID = Convert.ToInt32(TaskDbContext.GetByWmsTaskIdAsync(canceltask.taskID).TesTaskID);
                    ToWcsPublicReturnValueL retrunMessage = ToTesApiService.cancelTask(testaskID);

                    if (retrunMessage.ReturnCode != 0)
                    {
                        results.Add(new
                        {
                            taskID = canceltask.taskID,
                            success = false,
                            message = retrunMessage.ReturnMsg
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            taskID = canceltask.taskID,
                            success = true,
                            message = "succ"
                        });
                    }
                }

                var failedTasks = results.Where(r => !((dynamic)r).success).ToList();
                if (failedTasks.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"部分任务处理失败:" + string.Join(",", results)));
                }

                return Ok(ToWcsPublicReturnValue.Success("所有任务处理成功:" + string.Join(",", results)));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ToWcsPublicReturnValue.Error($"系统错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// 更新任务优先级
        /// </summary>
        [HttpPost("UpdateTaskPriority")]
        public async Task<IActionResult> UpdateTaskPriority([FromBody] JObject jsonInput)
        {
            try
            {
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("taskID", out var taskIDMess))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 taskID 字段"));
                }
                if (!jsonInput.TryGetValue("priority", out var priorityMess))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 priority 字段"));
                }

                var canceltask = jsonInput.ToObject<UpdateTaskPriorityModelToWMS>();
                UpdateTaskPriorityModel testaskID = new UpdateTaskPriorityModel();
                testaskID.taskID = Convert.ToInt32(TaskDbContext.GetByWmsTaskIdAsync(canceltask.taskID).TesTaskID);
                ToWcsPublicReturnValueL retrunMessage = ToTesApiService.updateTaskPriority(testaskID);
                if (retrunMessage.ReturnUserMsgg.Equals("成功"))
                {
                    return Ok(ToWcsPublicReturnValue.Success("succ"));
                }
                return BadRequest(ToWcsPublicReturnValue.Error($"更新优先级失败: {retrunMessage.ReturnUserMsgg}"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ToWcsPublicReturnValue.Error($"系统错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// 任务暂停
        /// </summary>
        [HttpPost("PauseTasks")]
        public async Task<IActionResult> PauseTasks([FromBody] List<PauseTasksModelToWMS> tasklist)
        {
            try
            {
                if (tasklist == null || !tasklist.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空或任务列表为空"));
                }

                foreach (var task in tasklist)
                {
                    if (string.IsNullOrEmpty(task.taskID))
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("缺少 taskID 字段"));
                    }
                }

                PauseTasksModel testaskID = new PauseTasksModel();
                string taskids = "";
                foreach (var task in tasklist)
                {
                    taskids += TaskDbContext.GetByWmsTaskIdAsync(task.taskID).TesTaskID;
                }
                testaskID.taskIDs = taskids;
                ToWcsPublicReturnValueL retrunMessage = ToTesApiService.pauseTasks(testaskID);
                if (retrunMessage.ReturnUserMsgg.Equals("成功"))
                {
                    return Ok(ToWcsPublicReturnValue.Success("succ"));
                }
                return BadRequest(ToWcsPublicReturnValue.Error($"任务暂停失败: {retrunMessage.ReturnUserMsgg}"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ToWcsPublicReturnValue.Error($"系统错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// 任务恢复
        /// </summary>
        [HttpPost("ResumeTasks")]
        public async Task<IActionResult> ResumeTasks([FromBody] List<PauseTasksModelToWMS> tasklist)
        {
            try
            {
                if (tasklist == null || !tasklist.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空或任务列表为空"));
                }

                foreach (var task in tasklist)
                {
                    if (string.IsNullOrEmpty(task.taskID))
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("缺少 taskID 字段"));
                    }
                }

                PauseTasksModel testaskID = new PauseTasksModel();
                string taskids = "";
                foreach (var task in tasklist)
                {
                    taskids += TaskDbContext.GetByWmsTaskIdAsync(task.taskID).TesTaskID;
                }
                testaskID.taskIDs = taskids;
                ToWcsPublicReturnValueL retrunMessage = ToTesApiService.resumeTasks(testaskID);
                if (retrunMessage.ReturnUserMsgg.Equals("成功"))
                {
                    return Ok(ToWcsPublicReturnValue.Success("succ"));
                }
                return BadRequest(ToWcsPublicReturnValue.Error($"任务恢复失败: {retrunMessage.ReturnUserMsgg}"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ToWcsPublicReturnValue.Error($"系统错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// 修改流向
        /// </summary>
        [HttpPost("SetStationStatus")]
        public async Task<IActionResult> SetStationStatus([FromBody] JObject jsonInput)
        {
            try
            {
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("stationCode", out var taskIDMess))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 taskID 字段"));
                }
                if (!jsonInput.TryGetValue("status", out var priorityMess))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 priority 字段"));
                }

                var stationStatus = jsonInput.ToObject<TessetStationStatus>();
                stationStatus.stationCode = StationstatusCollection.DataList.FirstOrDefault(x => x.WmsStationName == stationStatus.stationCode).StationCode;
                StationReturn retrunMessage = toTesApiService.setStationStatus(stationStatus);
                if (retrunMessage.ReturnUserMsg.Equals("成功"))
                {
                    return Ok(retrunMessage);
                }
                return BadRequest(ToWcsPublicReturnValue.Error($"修改流向失败: {retrunMessage.ReturnUserMsg}"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ToWcsPublicReturnValue.Error($"系统错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// 重试任务
        /// </summary>
        [HttpPost("retryTask")]
        public async Task<IActionResult> retryTask([FromBody] List<retryTaskModel> retryTasks)
        {
            try
            {
                if (retryTasks == null || !retryTasks.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空或任务列表为空"));
                }

                foreach (var jsonInput in retryTasks)
                {
                    if (jsonInput == null)
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                    }
                }

                var results = new List<object>();
                foreach (var retryTask in retryTasks)
                {
                    TESretryTaskModel testaskID = new TESretryTaskModel();
                    testaskID.taskID = Convert.ToInt32(TaskDbContext.GetByWmsTaskIdAsync(retryTask.taskID).TesTaskID);
                    ToWcsPublicReturnValueL retrunMessage = ToTesApiService.retryTask(testaskID);

                    if (retrunMessage.ReturnCode != 0)
                    {
                        results.Add(new
                        {
                            taskID = retryTask.taskID,
                            success = false,
                            message = retrunMessage.ReturnMsg
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            taskID = retryTask.taskID,
                            success = true,
                            message = "succ"
                        });
                    }
                }

                var failedTasks = results.Where(r => !((dynamic)r).success).ToList();
                if (failedTasks.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"部分任务处理失败:" + string.Join(",", results)));
                }

                return Ok(ToWcsPublicReturnValue.Success("所有任务处理成功:" + string.Join(",", results)));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ToWcsPublicReturnValue.Error($"系统错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// 获取所有存储点位信息（含容器号）
        /// </summary>
        [HttpGet("getAllStoragelocations")]
        public async Task<IActionResult> GetAllStoragelocations()
        {
            try
            {
                await PodSyncService.SyncPodIDsAsync();
            }
            catch (Exception syncEx)
            {
                return StatusCode(500, ToWcsPublicReturnValueL.Error("同步容器号失败", syncEx.Message));
            }

            try
            {
                var locations = StoragelocationCollection.DataList;
                return Ok(ToWcsPublicReturnValueL.Success("succ", "成功", locations));
            }
            catch (Exception returnEx)
            {
                return StatusCode(500, ToWcsPublicReturnValueL.Error("返回数据失败", returnEx.Message));
            }
        }
    }
}