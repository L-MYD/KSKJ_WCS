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
        ToWmsApiService toWmsApiService = new ToWmsApiService();

        /// <summary>
        /// WMS发送创建任务接口（批量）
        /// </summary>
        /// <param name="podList">任务列表</param>
        /// <returns>处理结果</returns>
        [HttpPost("newMovePodTask")]
        public async Task<IActionResult> NewMovePodTask([FromBody] List<newMoveTaskModel> podList)
        {
            try
            {
                // 1. 基础验证 - 检查集合是否为空
                if (podList == null || !podList.Any())
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空或任务列表为空"));
                }

                // 2. 验证必要字段 - 遍历集合检查每个对象的必需字段
                foreach (var pod in podList)
                {
                    if (string.IsNullOrEmpty(pod.wmsId))
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("缺少 taskID 字段"));
                    }
                    if (string.IsNullOrEmpty(pod.podId))
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("缺少 containerNo 字段"));
                    }
                    if (string.IsNullOrEmpty(pod.desPosition))
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("缺少 destPosition 字段"));
                    }
                    if (string.IsNullOrEmpty(pod.wmsTaskMessType))
                    {
                        return BadRequest(ToWcsPublicReturnValue.Error("缺少 wmsTaskMessType 字段"));
                    }
                }

                // ★★★ 新增：生成批次ID，注册本批次所有任务到缓存 ★★★
                string batchId = Guid.NewGuid().ToString("N");
                var taskIds = podList.Select(p => p.wmsId).ToList();
                CallbackCache.RegisterBatch(batchId, taskIds);

                // 处理每个任务（toWmsApiService.newMoveTask 是同步方法，等待TES创建成功）
                var results = new List<object>();
                foreach (var pod in podList)
                {
                    var tesTaskID = toWmsApiService.newMoveTask(pod);

                    if (tesTaskID.returnCode != 0)
                    {
                        results.Add(new
                        {
                            taskID = pod.wmsId,
                            success = false,
                            message = tesTaskID.returnMsg
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            taskID = pod.wmsId,
                            success = true,
                            message = "succ"
                        });
                    }
                }

                // 检查是否有失败的任务
                var failedTasks = results.Where(r => !((dynamic)r).success).ToList();
                IActionResult finalResponse;
                if (failedTasks.Any())
                {
                    finalResponse = BadRequest(ToWcsPublicReturnValue.Error($"部分任务处理失败:" + string.Join(",", results)));
                }
                else
                {
                    finalResponse = Ok(ToWcsPublicReturnValue.Success("所有任务处理成功:" + string.Join(",", results)));
                }

                // ★★★ 关键：在HTTP响应发送完成后，补发本批次所有缓存的回调 ★★★
                // 使用 OnCompleted 确保在响应发送之后执行补发
                HttpContext.Response.OnCompleted(() =>
                {
                    // 取出本批次所有缓存回调
                    var callbacks = CallbackCache.DequeueBatch(batchId);
                    if (callbacks.Any())
                    {
                        // 异步补发，不阻塞响应（丢给线程池）
                        Task.Run(async () =>
                        {
                            foreach (var cb in callbacks)
                            {
                                try
                                {
                                    // 调用 WMS 状态更新接口
                                    await toTesApiService.GetWmsreceiveTask(cb.Request);
                                }
                                catch (Exception ex)
                                {
                                    // 记录补发失败日志（可扩展为日志组件）
                                    Console.WriteLine($"补发回调失败，任务ID: {cb.TaskId}, 错误: {ex.Message}");
                                }
                            }
                        });
                    }
                    return Task.CompletedTask;
                });

                // 返回最终响应
                return finalResponse;
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