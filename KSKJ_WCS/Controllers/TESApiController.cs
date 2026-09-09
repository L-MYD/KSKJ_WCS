using Microsoft.AspNetCore.Mvc;
using System;
using WCS_Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using WCS_Models.TESModel;
using WCS_Services;
using WCS_Models.SqlModel;
using WCS_Helper;
using WCS_Models.WMSModel;
using System.Reflection.Metadata;

namespace WCS_Api.Controllers
{
    [Route("[controller]")]
    public class TESApiController : ControllerBase
    {
        ToTesApiService service = new ToTesApiService();
        ToWmsApiService wmsApiService = new ToWmsApiService();
        #region TES回调WCS接口
        /// <summary>
        /// 任务状态更新消息通知messageType=10
        /// 根据返回的任务状态同步更新发送给WMS
        /// </summary>
        /// <param name="jsonInput">JSON输入</param>
        /// <returns>处理结果</returns>
        [HttpPost("GetMessageType10")]
        public async Task<IActionResult> GetMessageType10([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                // 2. 验证必要字段
                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型10的消息
                if (messageTypeToken.ToString() != "10")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 5. 反序列化内容部分
                var content = contentToken.ToObject<TaskInfo>();
                if (content == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("content 反序列化失败"));
                }

                // 6. 提取关键信息（安全方式）

                string podId = content.PodID;
                string nodeId = content.PodInfo.NodeID;
                // 7. 反序列化完整参数
                var parm = jsonInput.ToObject<ToWcsPublicParameters>();
                if (parm == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("参数反序列化失败"));
                }

                //发送给WCS
                await service.receiveTask(parm, content);

                // 8. 返回成功响应
                return Ok(ToWcsPublicReturnValue.Success("succ"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 托盘线站点外检消息messageType=50
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("GetMessageType50")]
        public async Task<IActionResult> GetMessageType50([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                // 2. 验证必要字段
                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型52的消息
                if (messageTypeToken.ToString() != "50")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 5. 反序列化内容部分
                var content = contentToken.ToObject<InspectionContent>();
                if (content == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("content 反序列化失败"));
                }

                // 6. 验证signal结构
                if (content.Signal == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 signal 数据"));
                }

                // 7. 提取关键信息（安全方式）
                string podId = content.Signal.BarCode;
                string barCode = content.Signal?.BarCode;
                int errorCode = content.Signal?.ErrorCode ?? 0;

                // 8. 验证错误代码和原因
                if (errorCode != 0 && (content.Signal?.ErrorReasons == null || !content.Signal.ErrorReasons.Any()))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("错误代码非0时必须有errorReason"));
                }

                // 9. 反序列化完整参数
                var parm = jsonInput.ToObject<ToWcsPublicParameters>();
                if (parm == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("参数反序列化失败"));
                }
                await service.receiveTaskPallent(parm, content);
                // 10. 返回成功响应
                return Ok(ToWcsPublicReturnValue.Success("succ"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 轨道站点外检消息messageType=52
        /// </summary>
        /// <param name="jsonInput">JSON输入</param>
        /// <returns>处理结果</returns>
        [HttpPost("GetMessageType52")]
        public async Task<IActionResult> GetMessageType52([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                // 2. 验证必要字段
                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型52的消息
                if (messageTypeToken.ToString() != "52")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 5. 反序列化内容部分
                var content = contentToken.ToObject<InspectionContent>();
                if (content == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("content 反序列化失败"));
                }

                // 6. 验证result结构
                if (content == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 result 数据"));
                }

                // 7. 提取关键信息（安全方式）
                string podId = content.Signal.BarCode;
                string barCode = content.Signal?.BarCode;
                int errorCode = content.Signal?.ErrorCode ?? 0;

                // 8. 验证错误代码和原因
                if (errorCode != 0 && (content.Signal?.ErrorReasons == null || !content.Signal.ErrorReasons.Any()))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("错误代码非0时必须有errorReason"));
                }

                // 9. 反序列化完整参数
                var parm = jsonInput.ToObject<ToWcsPublicParameters>();
                if (parm == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("参数反序列化失败"));
                }
                //await service.receiveTaskPallent(parm, content);
                // 10. 返回成功响应
                return Ok(ToWcsPublicReturnValue.Success("succ"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 站点流向消息messageType=61
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("GetMessageType61")]
        public async Task<IActionResult> GetMessageType61([FromBody] JObject jsonInput)
        {
            try
            {
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                if (messageTypeToken.ToString() == "61")
                {
                    // 直接反序列化为目标参数对象
                    ToWcsPublicParameters parm = jsonInput.ToObject<ToWcsPublicParameters>();
                    MessageType61 content = contentToken.ToObject<MessageType61>();
                    //await service.UpdateStationOperation(content);
                    return Ok(ToWcsPublicReturnValue.Success("succ"));
                }

                return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 拆叠盘站点消息MessageType51
        /// </summary>
        /// <param name="jsonInput"></param>
        /// <returns></returns>
        [HttpPost("GetMessageType51")]
        public async Task<IActionResult> GetMessageType51([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型51的消息
                if (messageTypeToken.ToString() != "51")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 直接反序列化为目标参数对象
                ToWcsPublicParameters parm = jsonInput.ToObject<ToWcsPublicParameters>();
                MessageType51 content = contentToken.ToObject<MessageType51>();
                return Ok(ToWcsPublicReturnValue.Success("succ"));

            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 站点容器消息messageType=60
        /// </summary>
        /// <param name="jsonInput"></param>
        /// <returns></returns>
        [HttpPost("GetMessageType60")]
        public async Task<IActionResult> GetmessageType60([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型51的消息
                if (messageTypeToken.ToString() != "60")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 直接反序列化为目标参数对象
                ToWcsPublicParameters parm = jsonInput.ToObject<ToWcsPublicParameters>();
                MessageType60 content = contentToken.ToObject<MessageType60>();
                await service.GetStationMess(content);
                return Ok(ToWcsPublicReturnValue.Success("succ"));

            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 小车顶起容器消息messageType=72
        /// </summary>
        /// <param name="jsonInput"></param>
        /// <returns></returns>
        [HttpPost("GetMessageType72")]
        public async Task<IActionResult> GetMessageType72([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型72的消息
                if (messageTypeToken.ToString() != "72")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 直接反序列化为目标参数对象
                ToWcsPublicParameters parm = jsonInput.ToObject<ToWcsPublicParameters>();
                MessageType72 content = contentToken.ToObject<MessageType72>();
                return Ok(ToWcsPublicReturnValue.Success("succ"));

            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        /// <summary>
        /// 储位状态更新消息messageType=100
        /// </summary>
        /// <param name="jsonInput"></param>
        /// <returns></returns>
        [HttpPost("GetMessageType100")]
        public async Task<IActionResult> GetMessageType100([FromBody] JObject jsonInput)
        {
            try
            {
                // 1. 基础验证
                if (jsonInput == null)
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("请求体不能为空"));
                }

                if (!jsonInput.TryGetValue("messageType", out var messageTypeToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 messageType 字段"));
                }

                // 3. 只处理类型72的消息
                if (messageTypeToken.ToString() != "100")
                {
                    return BadRequest(ToWcsPublicReturnValue.Error($"不支持的消息类型: {messageTypeToken}"));
                }

                // 4. 验证content结构
                if (!jsonInput.TryGetValue("content", out var contentToken))
                {
                    return BadRequest(ToWcsPublicReturnValue.Error("缺少 content 字段"));
                }

                // 直接反序列化为目标参数对象
                ToWcsPublicParameters parm = jsonInput.ToObject<ToWcsPublicParameters>();
                MessageType100 content = contentToken.ToObject<MessageType100>();
                return Ok(ToWcsPublicReturnValue.Success("succ"));

            }
            catch (JsonException jsonEx)
            {
                return BadRequest(ToWcsPublicReturnValue.Error($"JSON解析错误: {jsonEx.Message}"));
            }
        }

        #endregion

    }
}
