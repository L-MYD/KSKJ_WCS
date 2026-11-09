using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCS_Models.WMSModel;
using WCS_Services;

namespace WCS_Api.Controllers
{
    /// <summary>
    /// WMS 对接接口（按《WMS-WCS对接协议》实现）
    /// </summary>
    [Route("api/WCS")]
    public class WMSApiController : Controller
    {
        ToWmsApiService toWmsApiService = new ToWmsApiService();

        /// <summary>
        /// nan_T 2026-11-07：协议字段必须保持 PascalCase（WmsId 等），
        /// 不能走 MVC 全局 CamelCase 解析器，统一手动序列化后以 Content 返回。
        /// </summary>
        private IActionResult WmsJson(object data, int statusCode = 200)
        {
            string json = JsonConvert.SerializeObject(data);
            var result = Content(json, "application/json", Encoding.UTF8);
            result.StatusCode = statusCode;
            return result;
        }

        /// <summary>
        /// WMS 任务下发：WMS-->WCS
        /// 请求地址：http://XXXXXXXXX/api/WCS/WMSTaskDis
        /// </summary>
        /// <param name="tasklist">任务列表</param>
        /// <returns>每条任务的处理结果</returns>
        [HttpPost("WMSTaskDis")]
        public async Task<IActionResult> WMSTaskDis([FromBody] List<WMSTaskDisModel> tasklist)
        {
            try
            {
                // 1. 基础验证
                if (tasklist == null || !tasklist.Any())
                {
                    return WmsJson(new List<WcsReturn>
                    {
                        new WcsReturn
                        {
                            msgType = "F",
                            msgText = "请求体不能为空或任务列表为空",
                            WmsId = null
                        }
                    }, 400);
                }

                // 2. 处理每个任务
                var results = new List<WcsReturn>();

                for (int i = 0; i < tasklist.Count; i++)
                {
                    var task = tasklist[i];
                    var taskIndex = i + 1;

                    // 验证当前任务的必需字段（协议：WmsId、Palno、ToLocation 必填）
                    if (string.IsNullOrEmpty(task.WmsId))
                    {
                        results.Add(new WcsReturn
                        {
                            msgType = "F",
                            msgText = $"第{taskIndex}个任务缺少 WmsId 字段",
                            WmsId = null
                        });
                        continue;
                    }

                    if (string.IsNullOrEmpty(task.Palno))
                    {
                        results.Add(new WcsReturn
                        {
                            msgType = "F",
                            msgText = $"第{taskIndex}个任务缺少 Palno 字段",
                            WmsId = task.WmsId
                        });
                        continue;
                    }

                    if (string.IsNullOrEmpty(task.ToLocation))
                    {
                        results.Add(new WcsReturn
                        {
                            msgType = "F",
                            msgText = $"第{taskIndex}个任务缺少 ToLocation 字段",
                            WmsId = task.WmsId
                        });
                        continue;
                    }

                    // 调用服务处理任务
                    try
                    {
                        var wcsResult = toWmsApiService.newMoveTask(task);
                        results.Add(wcsResult);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new WcsReturn
                        {
                            msgType = "E",
                            msgText = $"第{taskIndex}个任务处理异常: {ex.Message}",
                            WmsId = task.WmsId
                        });
                    }
                }

                // 3. 直接返回所有任务的结果列表
                return WmsJson(results);
            }
            catch (JsonException jsonEx)
            {
                return WmsJson(new List<WcsReturn>
                {
                    new WcsReturn
                    {
                        msgType = "F",
                        msgText = $"JSON解析错误: {jsonEx.Message}",
                        WmsId = null
                    }
                }, 400);
            }
            catch (Exception ex)
            {
                return WmsJson(new List<WcsReturn>
                {
                    new WcsReturn
                    {
                        msgType = "E",
                        msgText = $"系统错误: {ex.Message}",
                        WmsId = null
                    }
                }, 500);
            }
        }
    }
}
