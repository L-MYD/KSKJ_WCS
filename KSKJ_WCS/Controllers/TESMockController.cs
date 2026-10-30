using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using WCS_Models.TESModel;
using WCS_Services;

namespace WCS_Api.Controllers
{
    /// <summary>
    /// nan_T 2026-09-14：模拟 TES 服务端点（仅联调/测试环境使用）。
    /// 真实项目中 TES 由设备系统提供；此处模拟 TES 的创建搬运任务接口（tes/apiv2/newMovePodTask），
    /// 并在响应返回后自动回传任务状态（开始→完成），驱动 WCS 回传 WMS，形成"WMS下发→WCS执行→回调WMS"完整闭环。
    /// </summary>
    [Route("tes/apiv2")]
    public class TESMockController : ControllerBase
    {
        // nan_T 2026-09-14：基于时间戳的递增任务号种子，保证模拟 taskID 唯一
        private static long _seed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private static int NextTaskId() => (int)Interlocked.Increment(ref _seed);

        private readonly ToTesApiService _service = new ToTesApiService();

        /// <summary>
        /// nan_T 2026-09-14：模拟 TES 创建搬运任务：返回成功并生成任务ID，响应返回后自动模拟回传任务状态。
        /// </summary>
        [HttpPost("newMovePodTask")]
        public IActionResult NewMovePodTask([FromBody] TesNewMovePodTaskModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new TaskReqsonModel { returnCode = 1, returnMsg = "请求体不能为空", returnUserMsg = "请求体不能为空" });

                int taskId = NextTaskId();
                Console.WriteLine($"[TES-Mock] 创建任务 taskID={taskId}, PodID={model.PodID}, Destination={model.Destination}");

                // nan_T 2026-09-14：响应返回后异步模拟 TES 依次回传：任务开始(status=2) → 任务完成(status=4)
                Response.OnCompleted(() =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            // nan_T 2026-09-14：首次回传延迟2秒，确保 WCS 已完成 TES 任务映射入库（TescorrespondsWms），
                            // 否则 WCS 按 TES taskID 查询映射时会因记录尚未写入而回传失败
                            await Task.Delay(2000);
                            await SimulateCallbackAsync(taskId, model.PodID, status: 2);
                            await Task.Delay(3000);
                            await SimulateCallbackAsync(taskId, model.PodID, status: 4);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TES-Mock] 模拟回传失败: {ex.Message}");
                        }
                    });
                    return Task.CompletedTask;
                });

                return Ok(new TaskReqsonModel
                {
                    returnCode = 0,
                    returnMsg = "成功",
                    returnUserMsg = "成功",
                    data = new DataModel { taskID = taskId }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TES-Mock] 创建任务异常: {ex.Message}");
                return StatusCode(500, new TaskReqsonModel { returnCode = -1, returnMsg = $"系统错误: {ex.Message}", returnUserMsg = "系统内部错误" });
            }
        }

        /// <summary>
        /// nan_T 2026-09-14：构造并发送 TES 任务状态消息（messageType=10）给 WCS，由 WCS 接收后转发 WMS。
        /// </summary>
        private async Task SimulateCallbackAsync(int taskId, string podId, int status)
        {
            var content = new TaskInfo
            {
                TaskID = taskId.ToString(),
                Status = status,
                PodID = podId ?? "",
                PodInfo = new PodInfo { PositionType = 2, NodeID = "" },
                FinishTime = status == 4 ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : ""
            };
            var parm = new ToWcsPublicParameters
            {
                WarehouseID = "WH01",
                MessageType = 10,
                MessageID = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CreateTime = DateTime.Now.ToString("o"),
                Content = content
            };
            Console.WriteLine($"[TES-Mock] 回传任务状态 taskID={taskId}, status={status}");
            await _service.receiveTask(parm, content);
        }
    }
}
