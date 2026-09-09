using WCS_Models.TESModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WCS_Models;
using WCS_Models.WMSModel;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using WCS_Helper.Mapper;
using WCS_Models.SqlModel;
//using WCS_Common.Enum;
using System.Diagnostics.Eventing.Reader;
using System.Collections;
using WCS_Models.PLCModel;
using WCS_Models.WCSModel;
using WCS_Models.WCSModel.LogModel;
using System.Security.Cryptography;
//using S7.Net;
using System.Text.RegularExpressions;
using static WCS_Services.AislesCollection;

using WCS_IServices;
namespace WCS_Services
{
    public class ToTesApiService : IToTesApiService
    {
        string wmsapiip = ApiServerCollection.DataList.FirstOrDefault(x=>x.Name=="WMS").IP;
        static string tesapiip = ApiServerCollection.DataList.FirstOrDefault(x => x.Name == "TES").IP;
        /// <summary>
        /// 从 ErrorReason 中提取托盘号（使用正则表达式，支持多种括号格式）
        /// </summary>
        /// <param name="errorReason">错误原因字符串</param>
        /// <returns>提取的托盘号，如果不符合条件则返回空字符串</returns>
        public static string ExtractPodIdFromErrorReasonRegex(string errorReason)
        {
            // 使用统一的日志方法
            void WriteLog(string message, bool isError = false) => LogHelper.WriteLog("ExtractPodIdFromErrorReasonRegex", message, isError);

            try
            {
                WriteLog($"开始解析 ErrorReason: {errorReason}");

                // 1. 检查 ErrorReason 是否为空或空白
                if (string.IsNullOrWhiteSpace(errorReason))
                {
                    WriteLog("ErrorReason 为空，无法提取托盘号");
                    return string.Empty;
                }

                // 2. 使用正则表达式匹配大括号 {} 或圆括号 () 中的内容
                // 匹配模式：{内容} 或 (内容)
                var pattern = @"[{(]([^)}]+)[})]";
                var match = System.Text.RegularExpressions.Regex.Match(errorReason, pattern);

                if (match.Success)
                {
                    string extractedPodId = match.Groups[1].Value.Trim();
                    WriteLog($"使用正则表达式成功提取托盘号: {extractedPodId}");
                    return extractedPodId;
                }

                WriteLog("ErrorReason 中未找到大括号 {} 或圆括号 () 包裹的内容");
                return string.Empty;
            }
            catch (Exception ex)
            {
                WriteLog($"提取托盘号时发生异常: {ex.Message}", true);
                return string.Empty;
            }
        }

        /// <summary>
        /// 发送任务
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static TaskReqsonModel CreateMoveTask(newMoveTaskModel model, DesExt desExt, TaskExt tempte)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            void WriteLog(string message)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "CreateMoveTask_log.txt");
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }
            try
            {
                string serviceAddress = tesapiip + "tes/apiv2/newMovePodTask";

                // 记录HTTP请求
                TesNewMovePodTaskModel taskmodel = new TesNewMovePodTaskModel();
                taskmodel.PodID = model.podId;
                taskmodel.Destination = model.desPosition;
                taskmodel.SrcType = 1;
                //taskmodel.DesExt = new DesExt();
                //TaskExt tempte = new TaskExt();
                //tempte.AutoToRest = 1;
                //desExt.Unload = 1;
                taskmodel.DesExt = desExt;
                taskmodel.Priority = model.priority; //获取WMS下发的优先级
                taskmodel.TaskExt = tempte;//小车搬运后立即离开                
                //taskmodel.ReplacePodTask = ReplacePodTask;//允许替换托盘任务

                string requestJson = JsonConvert.SerializeObject(taskmodel);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES创建搬运任务API",
                    Module = "ToTesApiService",
                    Operation = "CreateMoveTask",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                WriteLog("参数为：" + requestJson);

                HttpClient client = new HttpClient();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel);
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                    dataStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string encoding = response.ContentEncoding;
                if (encoding == null || encoding.Length < 1)
                {
                    encoding = "UTF-8"; //默认编码  
                }

                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
                string retString = reader.ReadToEnd();

                // 记录HTTP响应
                var httpResponseLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Response",
                    Level = "Info",
                    Message = "TES创建搬运任务API响应",
                    Module = "ToTesApiService",
                    Operation = "CreateMoveTask",
                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpResponseLog);

                // 将JSON字符串反序列化为TaskReqsonModel对象
                TaskReqsonModel result = JsonConvert.DeserializeObject<TaskReqsonModel>(retString);
                return result;
            }
            catch (WebException webEx) when (webEx.Response is HttpWebResponse httpResponse)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES创建搬运任务API调用异常",
                    Module = "ToTesApiService",
                    Operation = "CreateMoveTask",
                    Details = $"异常信息: {webEx.Message}\n堆栈跟踪: {webEx.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误响应
                string errorContent;
                using (var stream = webEx.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    errorContent = reader.ReadToEnd();
                }

                // 记录HTTP错误响应
                var errorResponseLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error Response",
                    Level = "Error",
                    Message = "TES创建搬运任务API错误响应",
                    Module = "ToTesApiService",
                    Operation = "CreateMoveTask",
                    Details = $"HTTP状态码: {(int)httpResponse.StatusCode}\n错误响应内容: {errorContent}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorResponseLog);

                return new TaskReqsonModel
                {
                    returnCode = (int)httpResponse.StatusCode,
                    returnMsg = $"HTTP错误: {httpResponse.StatusCode}",
                    returnUserMsg = errorContent
                };
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES创建搬运任务API系统异常",
                    Module = "ToTesApiService",
                    Operation = "CreateMoveTask",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                return new TaskReqsonModel
                {
                    returnCode = -1,
                    returnMsg = $"调用CreateMoveTask接口失败: {ex.Message}",
                    returnUserMsg = "系统内部错误"
                };
            }
        }

        /// <summary>
        /// 更新任务状态
        /// </summary>
        /// <summary>
        /// 更新任务状态（从TES回调接收任务状态）
        /// </summary>
        public async Task receiveTask(ToWcsPublicParameters model, TaskInfo content)
        {
            void WriteLog(string message, bool isError = false) => LogHelper.WriteLog("receiveTask", message, isError);

            try
            {
                model.Content = content;
                WriteLog("=== 开始处理 receiveTask ===");
                WriteLog($"接收到消息: messageID={model.MessageID}, taskID={content.TaskID}");

                WriteLog("开始查询数据库任务映射...");
                TescorrespondsWms trw = TaskDbContext.GetByTesTaskIdAsync(content.TaskID.ToString());

                if (trw == null)
                {
                    WriteLog($"错误: 未找到taskID {content.TaskID} 对应的数据库记录", true);
                    throw new Exception($"未找到taskID {content.TaskID} 对应的WMS任务映射");
                }

                WriteLog($"数据库查询成功: WmsTaskID={trw.WmsTaskID}, WmsTaskType={trw.WmsTaskType}");

                // 构建发送给WMS的状态更新数据
                UpdateTaskInnerData taskmodel = new UpdateTaskInnerData();
                taskmodel.reqCode = Guid.NewGuid().ToString("N");
                taskmodel.taskId = trw.WmsTaskID;
                string errorreason = "";
                WriteLog($"构建请求对象: wcsid={taskmodel.taskId}");
                int statu = content.Status;
                switch (statu)
                {
                    case 2: // 任务开始
                        taskmodel.taskStat = "1";
                        trw.Statu = "开始执行任务";
                        WriteLog("任务状态: 开始执行任务 (2 → 1)");
                        break;
                    case 4: // 任务完成
                        taskmodel.taskStat = "2";
                        trw.Statu = "任务完成";
                        WriteLog("任务状态: 任务完成 (4 → 2)");
                        break;
                    case 5: // 任务失败
                        if (content.ErrorCode == 1330012)
                        {
                            taskmodel.taskStat = "5";
                            trw.Statu = "空取";
                            taskmodel.errMsg = "空取";
                            WriteLog("任务状态: 空取 (5 → 3)");
                        }
                        else if (content.ErrorCode == 1330003)
                        {
                            taskmodel.taskStat = "4";
                            errorreason = ExtractPodIdFromErrorReasonRegex(content.ErrorReason);
                            WriteLog("被(%v)货架挡住，目标位置不可达 (5 → 4)重新申请分配目的地");
                        }
                        else
                        {
                            taskmodel.taskStat = "5";
                            trw.Statu = "其它异常";
                            taskmodel.errMsg = $"异常代码：{content.ErrorCode}，异常类容：{content.ErrorReason}";
                            WriteLog($"任务状态: 其它异常 (5 → 5)异常代码：{content.ErrorCode}，异常类容{content.ErrorReason}");
                        }
                        break;
                }

                UpdateTaskRequest resendmodel = new UpdateTaskRequest();
                resendmodel.strInMsg = JsonConvert.SerializeObject(taskmodel);

                // ★★★ 新增：判断是否需要缓存回调（避免在HTTP响应返回前转发） ★★★
                if (CallbackCache.IsPending(trw.WmsTaskID))
                {
                    // 该任务所属批次尚未返回HTTP响应，缓存此回调
                    CallbackCache.Enqueue(trw.WmsTaskID, resendmodel);
                    WriteLog($"任务 {trw.WmsTaskID} 的回调已缓存，待HTTP响应返回后补发");
                    // 不转发，直接返回
                    return;
                }

                // 否则正常转发给WMS
                ReturnWcs getwmsreceiveTaskreturn = await GetWmsreceiveTask(resendmodel);
                WriteLog($"WMS返回状态:{getwmsreceiveTaskreturn.rtnCode}");

                if (getwmsreceiveTaskreturn.rtnCode == "0000000")
                {
                    WriteLog($"更新任务状态:{trw.Statu}");
                    TescorrespondsWms tcm = new TescorrespondsWms
                    {
                        TesTaskID = content.TaskID.ToString(),
                        Statu = trw.Statu,
                        CompletionTime = DateTime.UtcNow,
                        TargetLocation = trw.TargetLocation,
                        WcsId = trw.WcsId
                    };

                    if (taskmodel.taskStat == "4")
                    {
                        PalnoApplyModel taskmodelre = new PalnoApplyModel();
                        taskmodelre.reqCode = trw.WcsId;
                        taskmodelre.carrierId = errorreason;
                        taskmodelre.carrierLoc = "YZFLG";
                        taskmodelre.eqptId = "";
                        taskmodelre.weight = "";

                        WriteLog($"构建移障请求对象: taskId={trw.WmsTaskID}, containerNo={trw.Podid}");
                        UpdateTaskRequest resendmodelre = new UpdateTaskRequest();
                        resendmodelre.strInMsg = JsonConvert.SerializeObject(taskmodelre);
                        ReturnWcs wmsToWcsReturn = await GetPalnoApply(resendmodelre);
                        if (wmsToWcsReturn.rtnCode == "0000000")
                        {
                            // success
                        }
                    }

                    int reupdate = TaskDbContext.UpdateStatusAsync(tcm);
                    WriteLog($"数据库更新结果: {reupdate} 条记录受影响");

                    Stationstatus stationstatus = StationstatusCollection.DataList.FirstOrDefault(x => x.StationCode == tcm.TargetLocation);
                    if (taskmodel.taskStat == "2")
                    {
                        if (stationstatus != null)
                        {
                            string tolocation = "";
                            if (trw.WmsTaskType == "FOLD")
                            {
                                if (stationstatus.TesCheckName == "6003")
                                    tolocation = "6010";
                                else if (stationstatus.TesCheckName == "6006")
                                    tolocation = "6011";
                                else if (stationstatus.TesCheckName == "6009")
                                    tolocation = "6012";

                                ConveSendMessCollection.Add(new ConveSendMess
                                {
                                    MessType = "TT",
                                    PLCIP = "172.18.18.20",
                                    FromLocation = stationstatus.TesCheckName,
                                    ToLocation = tolocation,
                                    StUintID = "EMPTY" + trw.Podid,
                                    StUnitHeight = "",
                                    StUnitWeight = "",
                                    CanWrite = "01",
                                });
                            }
                            else
                            {
                                if (stationstatus.TesCheckName == "6006")
                                    tolocation = "6004";
                                else if (stationstatus.TesCheckName == "6003")
                                    tolocation = "6002";
                                else if (stationstatus.TesCheckName == "6009")
                                    tolocation = "6007";

                                ConveSendMessCollection.Add(new ConveSendMess
                                {
                                    MessType = "TO",
                                    PLCIP = "172.18.18.20",
                                    FromLocation = stationstatus.TesCheckName,
                                    ToLocation = tolocation,
                                    StUintID = trw.Podid,
                                    StUnitHeight = "",
                                    StUnitWeight = "",
                                    CanWrite = "01",
                                });
                            }
                            ConveSendMessCollection.Add(new ConveSendMess
                            {
                                MessType = "OM",
                                FromLocation = stationstatus.TesCheckName,
                                ToLocation = stationstatus.TesCheckName,
                            });
                        }
                    }
                    WriteLog("=== 处理成功完成 ===");
                }
            }
            catch (NullReferenceException nullEx)
            {
                // 异常处理（略，与原代码相同）
                throw;
            }
            catch (WebException webEx) when (webEx.Response is HttpWebResponse httpResponse)
            {
                // 异常处理（略）
                throw;
            }
            catch (JsonException jsonEx)
            {
                // 异常处理（略）
                throw;
            }
            catch (Exception ex)
            {
                // 异常处理（略）
                throw;
            }
        }

        /// <summary>
        /// 查询TES任务详情
        /// </summary>
        /// <param name="TesTskID">TES任务ID</param>
        public TaskInfo getTaskDetail(string TesTskID)
        {
            try
            {
                HttpClient client = new HttpClient();
                string serviceAddress = tesapiip + "/tes/apiv2/getTaskDetail";

                // 记录HTTP请求
                var requestModel = new getTaskDetail { taskID = TesTskID };
                string requestJson = JsonConvert.SerializeObject(requestModel);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES任务详情API",
                    Module = "ToTesApiService",
                    Operation = "getTaskDetail",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";
                getTaskDetail taskmodel = new getTaskDetail();
                taskmodel.taskID = TesTskID;
                string strJson = JsonConvert.SerializeObject(taskmodel);

                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                    dataStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string encoding = response.ContentEncoding;
                if (encoding == null || encoding.Length < 1)
                {
                    encoding = "UTF-8"; //默认编码  
                }

                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
                string retString = reader.ReadToEnd();

                // 记录HTTP响应
                var httpResponseLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Response",
                    Level = "Info",
                    Message = "TES任务详情API响应",
                    Module = "ToTesApiService",
                    Operation = "getTaskDetail",
                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpResponseLog);

                TaskInfo getTask = JsonConvert.DeserializeObject<TaskInfo>(retString);
                return getTask;
            }
            catch (Exception ex)
            {
                // 记录异常信息
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES任务详情API调用异常",
                    Module = "ToTesApiService",
                    Operation = "getTaskDetail",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);
                throw;
            }
        }

        /// <summary>
        /// tes托盘号站点绑定
        /// </summary>
        /// <param name="postPodModel"></param>
        /// <returns></returns>
        public static ContainerShelvingReqson addRongqizd(newMoveTaskModel wmstaskmodel)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/occupyStation";

                // 记录HTTP请求
                ContainerBinding taskmodel2 = new ContainerBinding();
                taskmodel2.warehouseID = "HETU";
                taskmodel2.requestID = wmstaskmodel.wmsId;
                taskmodel2.clientCode = "WCS";
                taskmodel2.podID = wmstaskmodel.podId;
                taskmodel2.layoutID = "600874436849041427";
                taskmodel2.stationCode = wmstaskmodel.currentPosition;
                taskmodel2.podFace = "3.14";//容器朝向

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES站点占用API",
                    Module = "ToTesApiService",
                    Operation = "addRongqizd",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES站点占用API响应",
                            Module = "ToTesApiService",
                            Operation = "addRongqizd",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson result = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES站点占用API调用异常",
                    Module = "ToTesApiService",
                    Operation = "addRongqizd",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES站点占用API错误响应",
                            Module = "ToTesApiService",
                            Operation = "addRongqizd",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ContainerShelvingReqson>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return new ContainerShelvingReqson
                            {
                                returnCode = (int)errorResponse.StatusCode,
                                returnMsg = errorContent,
                                returnUserMsg = $"HTTP错误: {errorResponse.StatusCode}"
                            };
                        }
                    }
                }

                // 其他类型的异常
                return new ContainerShelvingReqson
                {
                    returnCode = -1,
                    returnMsg = ex.Message,
                    returnUserMsg = "网络请求异常"
                };
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES站点占用API系统异常",
                    Module = "ToTesApiService",
                    Operation = "addRongqizd",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return new ContainerShelvingReqson
                {
                    returnCode = -1,
                    returnMsg = ex.Message,
                    returnUserMsg = "系统异常"
                };
            }
        }

        /// <summary>
        /// 清除站点
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public static ToWcsPublicReturnValueL releaseStation(string station)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/releaseStation";

                // 记录HTTP请求
                releaseStation taskmodel2 = new releaseStation();
                taskmodel2.RequestID = Guid.NewGuid().ToString("N");
                taskmodel2.stationCode = station;

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES释放站点API",
                    Module = "ToTesApiService",
                    Operation = "releaseStation",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES释放站点API响应",
                            Module = "ToTesApiService",
                            Operation = "releaseStation",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson parm = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        ToWcsPublicReturnValueL result = new ToWcsPublicReturnValueL(parm.returnCode, parm.returnMsg, parm.returnUserMsg, parm.data);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES释放站点API调用异常",
                    Module = "ToTesApiService",
                    Operation = "releaseStation",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES释放站点API错误响应",
                            Module = "ToTesApiService",
                            Operation = "releaseStation",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ToWcsPublicReturnValueL>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return ToWcsPublicReturnValueL.Error($"HTTP错误: {errorResponse.StatusCode}");
                        }
                    }
                }

                // 其他类型的异常
                return ToWcsPublicReturnValueL.Error("网络请求异常");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES释放站点API系统异常",
                    Module = "ToTesApiService",
                    Operation = "releaseStation",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return ToWcsPublicReturnValueL.Error("系统异常");
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="cancelTask"></param>
        /// <returns></returns>
        public static ToWcsPublicReturnValueL cancelTask(CancelTask cancelTask)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/cancelTask";

                // 记录HTTP请求
                CancelTask taskmodel2 = new CancelTask();
                taskmodel2 = cancelTask;
                taskmodel2.taskID = cancelTask.taskID;
                taskmodel2.RequestID = cancelTask.taskID.ToString();

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES取消任务API",
                    Module = "ToTesApiService",
                    Operation = "cancelTask",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES取消任务API响应",
                            Module = "ToTesApiService",
                            Operation = "cancelTask",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson parm = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        ToWcsPublicReturnValueL result = new ToWcsPublicReturnValueL(parm.returnCode, parm.returnMsg, parm.returnUserMsg, parm.data);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES取消任务API调用异常",
                    Module = "ToTesApiService",
                    Operation = "cancelTask",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES取消任务API错误响应",
                            Module = "ToTesApiService",
                            Operation = "cancelTask",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ToWcsPublicReturnValueL>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return ToWcsPublicReturnValueL.Error($"HTTP错误: {errorResponse.StatusCode}");
                        }
                    }
                }

                // 其他类型的异常
                return ToWcsPublicReturnValueL.Error("网络请求异常");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES取消任务API系统异常",
                    Module = "ToTesApiService",
                    Operation = "cancelTask",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return ToWcsPublicReturnValueL.Error("系统异常");
            }
        }

        /// <summary>
        /// 申请入库
        /// </summary>
        /// <returns></returns>
        public async Task receiveTaskPallent(ToWcsPublicParameters model, InspectionContent content)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            // 或者保留时间戳+Guid
            string getWCSTaskID()
            {
                long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                string guidPart = Guid.NewGuid().ToString("N").Substring(0, 4);
                return $"WCS{timestamp}{guidPart}";
            }


            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "receiveTaskPallent_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            try
            {
                model.Content = content;
                WriteLog("=== 开始处理 receiveTaskPallent ===");
                WriteLog($"接收到消息: messageID={model.MessageID}, 检测设备号={content.RobotId}");

                // 拿到对应的对象之后，我们就把这个对象中的wmsid,跟wms的类型一起拿到
                if (content.Signal.ErrorCode == 0)
                {
                    PalnoApplyModel taskmodel = new PalnoApplyModel();//新建一个发送给wms的消息对象
                    taskmodel.reqCode = getWCSTaskID();//任务id
                    taskmodel.carrierId = content.Signal.BarCode;//容器号
                    //WriteLog($"接收到消息: StationCode={content.StationCode}, WMSstationName={StationNameModelCollection.DataList.FirstOrDefault(x => x.TesCheckName == content.StationCode).WmsStationName} ,");
                    Stationstatus st = StationstatusCollection.DataList.FirstOrDefault(x => x.TesCheckName == content.StationCode && x.Status == "入库");

                    taskmodel.carrierLoc = st.WmsStationName;//站点号
                    taskmodel.eqptId = content.RobotId;
                    taskmodel.weight = content.Signal.SignalBody.WeightKg.ToString();
                    EmptyPodIDApply emptyPodIDApply = TaskDbContext.GetEmptyPodIDApplyByPodID(taskmodel.carrierId);
                    if (emptyPodIDApply != null)
                    {
                        taskmodel.prdQty = emptyPodIDApply.PodNum;
                        taskmodel.carrierDpj = "FOLD";
                    }

                    WriteLog($"构建请求对象: taskId={taskmodel.reqCode}, containerNo={taskmodel.carrierId}");

                    UpdateTaskRequest resendmodel = new UpdateTaskRequest();
                    resendmodel.strInMsg = JsonConvert.SerializeObject(taskmodel);

                    bool success = false;
                    ReturnWcs wmsToWcsReturn = new ReturnWcs();
                    wmsToWcsReturn = await GetPalnoApply(resendmodel);
                    string tolocation = "";
                    if (wmsToWcsReturn.rtnCode != "0000000")
                    {
                        if (wmsToWcsReturn.rtnMesg.Contains("存在未完成的任务,不能再次请求入库"))
                        {
                            return;
                        }
                        else
                        {
                            if (content.RobotId == "6006")
                            {
                                tolocation = "6005";
                            }
                            else if (content.RobotId == "6003")
                            {
                                tolocation = "6002";
                            }
                            else if (content.RobotId == "6009")
                            {
                                tolocation = "6008";
                            }
                            ConveSendMessCollection.Add(new ConveSendMess
                            {
                                MessType = "TO",
                                PLCIP = "172.18.18.20",
                                FromLocation = content.RobotId,
                                ToLocation = tolocation,
                                StUintID = content.Signal.BarCode,
                                StUnitHeight = "",
                                StUnitWeight = "",
                                CanWrite = "01",
                            });
                        }
                    }
                    if (wmsToWcsReturn.rtnCode == "0000000")
                    {
                        TaskDbContext.DeleteEmptyPodIDApply(taskmodel.carrierId);//成功后删除
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                // 记录异常信息
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "轨道申请入库处理异常",
                    Module = "ToTesApiService",
                    Operation = "receiveTaskPallent",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 原有的异常处理逻辑保持不变
                if (ex is NullReferenceException nullEx)
                {
                    string errorDetail = "空引用异常 - ";
                    if (content == null) errorDetail += "model.content 为null";
                    else if (content.Signal.BarCode == null) errorDetail += "content.Signal.BarCode托盘号 为null";
                    else errorDetail += nullEx.Message;

                    WriteLog($"{errorDetail}{Environment.NewLine}堆栈跟踪: {nullEx.StackTrace}", true);
                    throw new Exception($"空引用错误: {errorDetail}", nullEx);
                }
                else if (ex is WebException webEx && webEx.Response is HttpWebResponse httpResponse)
                {
                    string responseContent;
                    using (var stream = webEx.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        responseContent = reader.ReadToEnd();
                    }

                    WriteLog($"HTTP请求失败: {httpResponse.StatusCode} {httpResponse.StatusDescription}{Environment.NewLine}响应内容: {responseContent}{Environment.NewLine}堆栈跟踪: {webEx.StackTrace}", true);
                    throw new Exception($"WMS API调用失败: {httpResponse.StatusCode} - {responseContent}", webEx);
                }
                else if (ex is JsonException jsonEx)
                {
                    WriteLog($"JSON解析错误: {jsonEx.Message}{Environment.NewLine}堆栈跟踪: {jsonEx.StackTrace}", true);
                    throw new Exception("响应数据格式错误", jsonEx);
                }
                else
                {
                    string exceptionType = ex.GetType().FullName;
                    string errorMessage = $"{exceptionType}: {ex.Message}{Environment.NewLine}堆栈跟踪: {ex.StackTrace}";

                    // 根据异常类型提供更具体的错误信息
                    if (ex is ArgumentException) errorMessage = $"参数错误 - {ex.Message}";
                    else if (ex is IOException) errorMessage = $"IO操作错误 - {ex.Message}";
                    else if (ex is TimeoutException) errorMessage = $"请求超时 - {ex.Message}";

                    WriteLog(errorMessage, true);
                    throw new Exception($"处理任务时发生错误: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 修改AGV站点的流向
        /// </summary>
        public async Task UpdateStationOperation(MessageType61 content)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "UpdateStationOperation_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            // 记录接口参数
            string requestJson = JsonConvert.SerializeObject(content);
            var httpRequestLog = new LogModel
            {
                UserType = "http",
                LogType = "HTTP Request",
                Level = "Info",
                Message = "调用WMS API - 修改AGV站点的流向",
                Module = "ToTesApiService",
                Operation = "UpdateStationOperation",
                Details = $"消息接口: GetMessageType61\n完整请求参数JSON:\n{requestJson}",
                UserId = "System",
                IpAddress = "System",
                CreateTime = DateTime.UtcNow,
                IsArchived = "False"
            };
            LogsDbContext.Insert(httpRequestLog);

            string status = "";
            if(content.status==12)
            {
                status = "入库";
            }
            else if (content.status == 11)
            {
                status = "出库";
            }
            Stationstatus stationstatus = StationstatusCollection.DataList.Find(x => x.TesCheckName == content.stationCode && x.Status == status);
            if(stationstatus == null)
            {
                stationstatus = StationstatusCollection.DataList.Find(x => x.StationCode == content.stationCode && x.Status == status);
            }
            switch (content.status)
            {
                case 12: //入库流向
                    status = "入库";
                    
                    ConveSendMessCollection.Add(new ConveSendMess
                    {
                        MessType = "IM",
                        FromLocation = stationstatus.TesCheckName,
                        ToLocation = stationstatus.TesCheckName,
                    });
                    break;
                case 11://出库流向
                    status = "出库";
                    ConveSendMessCollection.Add(new ConveSendMess
                    {
                        MessType = "OM",
                        FromLocation = stationstatus.TesCheckName,
                        ToLocation = stationstatus.TesCheckName,
                    });
                    break;
                case 1: //禁用
                    status = "禁用";
                    break;
            }
            WriteLog($"原始参数：{stationstatus.Status},更新参数{status}");
            stationstatus.Status = status;
            int count = TaskDbContext.UpdateStationstatusAsync(stationstatus);
            if (count > 0)
            {
                WriteLog($"更新成功");
            }

        }

        /// <summary>
        /// 切换流向
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public StationReturn setStationStatus(TessetStationStatus model)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/setStationStatus";

                // 记录HTTP请求
                TessetStationStatus taskmodel = new TessetStationStatus
                {
                    stationCode = model.stationCode,
                    status = model.status,
                    ignoreChangeFlowCmd = 1
                };

                string requestJson = JsonConvert.SerializeObject(taskmodel);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES设置站点状态API",
                    Module = "ToTesApiService",
                    Operation = "setStationStatus",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel);

                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                    dataStream.Close();
                }

                // 处理可能的HTTP错误
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        string encoding = response.ContentEncoding;
                        if (string.IsNullOrEmpty(encoding))
                        {
                            encoding = "UTF-8";
                        }

                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                        {
                            string retString = reader.ReadToEnd();

                            // 记录HTTP响应
                            var httpResponseLog = new LogModel
                            {
                                UserType = "http",
                                LogType = "HTTP Response",
                                Level = "Info",
                                Message = "TES设置站点状态API响应",
                                Module = "ToTesApiService",
                                Operation = "setStationStatus",
                                Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                                UserId = "System",
                                IpAddress = "System",
                                CreateTime = DateTime.UtcNow,
                                IsArchived = "False"
                            };
                            LogsDbContext.Insert(httpResponseLog);

                            // 反序列化为 TaskReqsonModel
                            var result = JsonConvert.DeserializeObject<StationReturn>(retString);

                            // 确保不会返回null
                            return result ?? new StationReturn
                            {
                                ReturnCode = -1,
                                ReturnMsg = "反序列化返回null",
                                ReturnUserMsg = "系统错误",
                            };
                        }
                    }
                }
                catch (WebException webEx) when (webEx.Response is HttpWebResponse httpResponse)
                {
                    // 记录HTTP异常
                    var errorLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "HTTP Error",
                        Level = "Error",
                        Message = "TES设置站点状态API调用异常",
                        Module = "ToTesApiService",
                        Operation = "setStationStatus",
                        Details = $"异常信息: {webEx.Message}\n堆栈跟踪: {webEx.StackTrace}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(errorLog);

                    // 处理HTTP错误响应
                    string errorContent;
                    using (var stream = webEx.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        errorContent = reader.ReadToEnd();
                    }

                    // 记录HTTP错误响应
                    var errorResponseLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "HTTP Error Response",
                        Level = "Error",
                        Message = "TES设置站点状态API错误响应",
                        Module = "ToTesApiService",
                        Operation = "setStationStatus",
                        Details = $"HTTP状态码: {(int)httpResponse.StatusCode}\n错误响应内容: {errorContent}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(errorResponseLog);

                    return new StationReturn
                    {
                        ReturnCode = (int)httpResponse.StatusCode,
                        ReturnMsg = $"HTTP错误: {httpResponse.StatusCode}",
                        ReturnUserMsg = errorContent
                    };
                }
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES设置站点状态API系统异常",
                    Module = "ToTesApiService",
                    Operation = "setStationStatus",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return new StationReturn
                {
                    ReturnCode = -1,
                    ReturnMsg = $"调用setStationStatus接口失败: {ex.Message}",
                    ReturnUserMsg = "系统内部错误"
                };
            }
        }

        /// <summary>
        /// 调用WMS接收任务状态更新接口
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ReturnWcs> GetWmsreceiveTask(UpdateTaskRequest model)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "GetWmsreceiveTask_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            // 配置重试参数
            int maxRetries = 3; // 最大重试次数
            int retryDelayMs = 10000; // 重试延迟时间（毫秒）
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    // 记录HTTP请求 - 完整的请求信息
                    string serviceAddress = wmsapiip + "/matrix/sendMsg/WWCSAPI/S";
                    string requestJson = JsonConvert.SerializeObject(model);
                    var httpRequestLog = new LogModel
                    {
                        UserType = "http",
                        LogType = retryCount == 0 ? "HTTP Request" : $"HTTP Request Retry {retryCount}",
                        Level = "Info",
                        Message = retryCount == 0 ? "调用WMS API - 接收任务状态更新" : $"重试调用WMS API - 接收任务状态更新 (第{retryCount}次)",
                        Module = "ToTesApiService",
                        Operation = "GetWmsreceiveTask",
                        Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(httpRequestLog);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    string strJson = JsonConvert.SerializeObject(model);
                    WriteLog($"请求JSON: {strJson}");

                    // 写入请求体
                    using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                    {
                        dataStream.Write(strJson);
                        dataStream.Close();
                    }

                    WriteLog("请求发送成功，等待响应...");

                    // 发送请求并获取响应
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        string encoding = response.ContentEncoding;
                        if (string.IsNullOrEmpty(encoding))
                        {
                            encoding = "UTF-8"; //默认编码  
                        }

                        WriteLog($"收到响应，状态码: {(int)response.StatusCode} {response.StatusCode}");

                        if ((int)response.StatusCode == 200)
                        {
                            // 读取响应内容
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                            {
                                string retString = reader.ReadToEnd();
                                WriteLog($"响应内容: {retString}");

                                // 记录HTTP响应 - 完整的响应信息
                                var httpResponseLog = new LogModel
                                {
                                    UserType = "http",
                                    LogType = "HTTP Response",
                                    Level = "Info",
                                    Message = "WMS API响应 - 接收任务状态更新",
                                    Module = "ToTesApiService",
                                    Operation = "GetWmsreceiveTask",
                                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                                    UserId = "System",
                                    IpAddress = "System",
                                    CreateTime = DateTime.UtcNow,
                                    IsArchived = "False"
                                };
                                LogsDbContext.Insert(httpResponseLog);

                                var result = JObject.Parse(retString).ToObject<ReturnWcs>();
                                return result;
                            }
                        }
                        else
                        {
                            // 非200状态码，准备重试
                            WriteLog($"收到非200状态码: {(int)response.StatusCode}，准备重试", true);

                            // 如果已达最大重试次数，则抛出异常
                            if (retryCount >= maxRetries)
                            {
                                throw new Exception($"达到最大重试次数({maxRetries})，最后一次状态码: {(int)response.StatusCode}");
                            }

                            // 等待指定时间后重试
                            retryCount++;
                            WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                            await Task.Delay(retryDelayMs);
                        }
                    }
                }
                catch (WebException webEx)
                {
                    // 处理网络异常
                    WriteLog($"网络请求异常: {webEx.Message}", true);

                    // 如果已达最大重试次数，则抛出异常
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"达到最大重试次数({maxRetries})，最后错误: {webEx.Message}");
                    }

                    // 等待指定时间后重试
                    retryCount++;
                    WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception ex)
                {
                    // 其他异常直接抛出
                    WriteLog($"发生未预期异常: {ex.Message}", true);
                    throw;
                }
            }

            // 理论上不会执行到这里，因为要么成功返回，要么抛出异常
            throw new Exception("未知错误：重试逻辑异常");
        }

        /// <summary>
        /// 请求容器入库终点
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ReturnWcs> GetPalnoApply(UpdateTaskRequest model)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "GetPalnoApply_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            // 配置重试参数
            int maxRetries = 3; // 最大重试次数
            int retryDelayMs = 10000; // 重试延迟时间（毫秒）
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    // 记录HTTP请求 - 完整的请求信息
                    string serviceAddress = wmsapiip + "/matrix/sendMsg/WWCSAPI/Q";
                    string requestJson = JsonConvert.SerializeObject(model);
                    var httpRequestLog = new LogModel
                    {
                        UserType = "http",
                        LogType = retryCount == 0 ? "HTTP Request" : $"HTTP Request Retry {retryCount}",
                        Level = "Info",
                        Message = retryCount == 0 ? "调用WMS API - 托盘入库申请" : $"重试调用WMS API - 托盘入库申请 (第{retryCount}次)",
                        Module = "ToTesApiService",
                        Operation = "GetPalnoApply",
                        Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(httpRequestLog);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    string strJson = JsonConvert.SerializeObject(model);
                    WriteLog($"请求JSON: {strJson}");

                    // 写入请求体
                    using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                    {
                        dataStream.Write(strJson);
                        dataStream.Close();
                    }

                    WriteLog("请求发送成功，等待响应...");

                    // 发送请求并获取响应
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        string encoding = response.ContentEncoding;
                        if (string.IsNullOrEmpty(encoding))
                        {
                            encoding = "UTF-8"; //默认编码  
                        }

                        WriteLog($"收到响应，状态码: {(int)response.StatusCode} {response.StatusCode}");

                        if ((int)response.StatusCode == 200)
                        {
                            // 读取响应内容
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                            {
                                string retString = reader.ReadToEnd();
                                WriteLog($"响应内容: {retString}");

                                // 记录HTTP响应 - 完整的响应信息
                                var httpResponseLog = new LogModel
                                {
                                    UserType = "http",
                                    LogType = "HTTP Response",
                                    Level = "Info",
                                    Message = "WMS API响应 - 托盘入库申请",
                                    Module = "ToTesApiService",
                                    Operation = "GetPalnoApply",
                                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                                    UserId = "System",
                                    IpAddress = "System",
                                    CreateTime = DateTime.UtcNow,
                                    IsArchived = "False"
                                };
                                LogsDbContext.Insert(httpResponseLog);

                                var result = JObject.Parse(retString).ToObject<ReturnWcs>();
                                return result;
                            }
                        }
                        else
                        {
                            // 非200状态码，准备重试
                            WriteLog($"收到非200状态码: {(int)response.StatusCode}，准备重试", true);

                            // 如果已达最大重试次数，则抛出异常
                            if (retryCount >= maxRetries)
                            {
                                throw new Exception($"达到最大重试次数({maxRetries})，最后一次状态码: {(int)response.StatusCode}");
                            }

                            // 等待指定时间后重试
                            retryCount++;
                            WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                            await Task.Delay(retryDelayMs);
                        }
                    }
                }
                catch (WebException webEx)
                {
                    // 处理网络异常
                    WriteLog($"网络请求异常: {webEx.Message}", true);

                    // 如果已达最大重试次数，则抛出异常
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"达到最大重试次数({maxRetries})，最后错误: {webEx.Message}");
                    }

                    // 等待指定时间后重试
                    retryCount++;
                    WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception ex)
                {
                    // 其他异常直接抛出
                    WriteLog($"发生未预期异常: {ex.Message}", true);
                    throw;
                }
            }

            // 理论上不会执行到这里，因为要么成功返回，要么抛出异常
            throw new Exception("未知错误：重试逻辑异常");
        }

        /// <summary>
        /// 请求母垛出库
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ReturnWcs> PostPalnoApply(UpdateEmptyPallet model)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "PostPalnoApply_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            // 配置重试参数
            int maxRetries = 3; // 最大重试次数
            int retryDelayMs = 10000; // 重试延迟时间（毫秒）
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    // 记录HTTP请求 - 完整的请求信息
                    string serviceAddress = wmsapiip + "/matrix/sendMsg/WWCSAPI/K";
                    string requestJson = JsonConvert.SerializeObject(model);
                    var httpRequestLog = new LogModel
                    {
                        UserType = "http",
                        LogType = retryCount == 0 ? "HTTP Request" : $"HTTP Request Retry {retryCount}",
                        Level = "Info",
                        Message = retryCount == 0 ? "调用WMS API - 请求母垛出库" : $"重试调用WMS API - 请求母垛出库 (第{retryCount}次)",
                        Module = "ToTesApiService",
                        Operation = "PostPalnoApply",
                        Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(httpRequestLog);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    string strJson = JsonConvert.SerializeObject(model);
                    WriteLog($"请求JSON: {strJson}");

                    // 写入请求体
                    using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                    {
                        dataStream.Write(strJson);
                        dataStream.Close();
                    }

                    WriteLog("请求发送成功，等待响应...");

                    // 发送请求并获取响应
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        string encoding = response.ContentEncoding;
                        if (string.IsNullOrEmpty(encoding))
                        {
                            encoding = "UTF-8"; //默认编码  
                        }

                        WriteLog($"收到响应，状态码: {(int)response.StatusCode} {response.StatusCode}");

                        if ((int)response.StatusCode == 200)
                        {
                            // 读取响应内容
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                            {
                                string retString = reader.ReadToEnd();
                                WriteLog($"响应内容: {retString}");

                                // 记录HTTP响应 - 完整的响应信息
                                var httpResponseLog = new LogModel
                                {
                                    UserType = "http",
                                    LogType = "HTTP Response",
                                    Level = "Info",
                                    Message = "WMS API响应 - 请求母垛出库",
                                    Module = "ToTesApiService",
                                    Operation = "PostPalnoApply",
                                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                                    UserId = "System",
                                    IpAddress = "System",
                                    CreateTime = DateTime.UtcNow,
                                    IsArchived = "False"
                                };
                                LogsDbContext.Insert(httpResponseLog);

                                var result = JObject.Parse(retString).ToObject<ReturnWcs>();
                                return result;
                            }
                        }
                        else
                        {
                            // 非200状态码，准备重试
                            WriteLog($"收到非200状态码: {(int)response.StatusCode}，准备重试", true);

                            // 如果已达最大重试次数，则抛出异常
                            if (retryCount >= maxRetries)
                            {
                                throw new Exception($"达到最大重试次数({maxRetries})，最后一次状态码: {(int)response.StatusCode}");
                            }

                            // 等待指定时间后重试
                            retryCount++;
                            WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                            await Task.Delay(retryDelayMs);
                        }
                    }
                }
                catch (WebException webEx)
                {
                    // 处理网络异常
                    WriteLog($"网络请求异常: {webEx.Message}", true);

                    // 如果已达最大重试次数，则抛出异常
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"达到最大重试次数({maxRetries})，最后错误: {webEx.Message}");
                    }

                    // 等待指定时间后重试
                    retryCount++;
                    WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception ex)
                {
                    // 其他异常直接抛出
                    WriteLog($"发生未预期异常: {ex.Message}", true);
                    throw;
                }
            }

            // 理论上不会执行到这里，因为要么成功返回，要么抛出异常
            throw new Exception("未知错误：重试逻辑异常");
        }

        /// <summary>
        /// 日志辅助类 - 消除重复的日志代码
        /// </summary>
        public static class LogHelper
        {
            public static void WriteLog(string operation, string message, bool isError = false)
            {
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    if (!Directory.Exists(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                    }

                    string logFilePath = Path.Combine(logPath, $"{operation}_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";

                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }
        }

        /// <summary>
        /// 切换站点交互状态setStationInteractiveStatus
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ToTesPublicReturnValue setStationInteractiveStatus(string stationCode,int interactiveStatus)
        {
            try
            {
                string serviceAddress = tesapiip + "tes/apiv2/setStationInteractiveStatus";

                // 记录HTTP请求
                SetStationInteractiveStatus taskmodel = new SetStationInteractiveStatus();
                taskmodel.stationCodes = stationCode;
                taskmodel.interactiveStatus = interactiveStatus;

                string requestJson = JsonConvert.SerializeObject(taskmodel);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES切换站点交互状态API",
                    Module = "ToTesApiService",
                    Operation = "setStationInteractiveStatus",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpClient client = new HttpClient();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel);
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                    dataStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string encoding = response.ContentEncoding;
                if (encoding == null || encoding.Length < 1)
                {
                    encoding = "UTF-8"; //默认编码  
                }

                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
                string retString = reader.ReadToEnd();

                // 记录HTTP响应
                var httpResponseLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Response",
                    Level = "Info",
                    Message = "TES切换站点交互状态API响应",
                    Module = "ToTesApiService",
                    Operation = "setStationInteractiveStatus",
                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpResponseLog);

                // 将JSON字符串反序列化为GetStationReturn对象
                GetStationReturn result = JsonConvert.DeserializeObject<GetStationReturn>(retString);
                return result;
            }
            catch (WebException webEx) when (webEx.Response is HttpWebResponse httpResponse)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES切换站点交互状态API调用异常",
                    Module = "ToTesApiService",
                    Operation = "setStationInteractiveStatus",
                    Details = $"异常信息: {webEx.Message}\n堆栈跟踪: {webEx.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误响应
                string errorContent;
                using (var stream = webEx.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    errorContent = reader.ReadToEnd();
                }

                // 记录HTTP错误响应
                var errorResponseLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error Response",
                    Level = "Error",
                    Message = "TES切换站点交互状态API错误响应",
                    Module = "ToTesApiService",
                    Operation = "setStationInteractiveStatus",
                    Details = $"HTTP状态码: {(int)httpResponse.StatusCode}\n错误响应内容: {errorContent}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorResponseLog);

                return new GetStationReturn
                {
                    ReturnCode = (int)httpResponse.StatusCode,
                    ReturnMsg = $"HTTP错误: {httpResponse.StatusCode}",
                    ReturnUserMsg = errorContent
                };
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES切换站点交互状态API系统异常",
                    Module = "ToTesApiService",
                    Operation = "setStationInteractiveStatus",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                return new GetStationReturn
                {
                    ReturnCode = -1,
                    ReturnMsg = $"调用setStationInteractiveStatus接口失败: {ex.Message}",
                    ReturnUserMsg = "系统内部错误"
                };
            }
        }

        /// <summary>
        /// 查询托盘列表
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task<ContainerReqsonInfo> SendAgvTaskAsync(ContainerInfoModel task)
        {
            // 记录HTTP请求开始
            var httpRequestLog = new LogModel
            {
                UserType = "http",
                LogType = "HTTP Request",
                Level = "Info",
                Message = "开始查询托盘列表",
                Module = "ToTesApiService",
                Operation = "SendAgvTaskAsync",
                Details = $"请求参数: {JsonConvert.SerializeObject(task)}",
                UserId = "System",
                IpAddress = "System",
                CreateTime = DateTime.UtcNow,
                IsArchived = "False"
            };
            LogsDbContext.Insert(httpRequestLog);

            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "SendAgvTask_log.txt");
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            WriteLog("=== 开始发送AGV任务请求 ===");

            if (task == null)
            {
                WriteLog("错误: 传入的task参数为null");

                // 记录参数错误
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "查询托盘列表参数错误",
                    Module = "ToTesApiService",
                    Operation = "SendAgvTaskAsync",
                    Details = "传入的task参数为null",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                return new ContainerReqsonInfo { returnCode = -1, returnMsg = "任务参数不能为空" };
            }

            WriteLog($"任务对象: {JsonConvert.SerializeObject(task, Formatting.Indented)}");

            string PostUrl = tesapiip + "tes/apiv2/getPodList";
            WriteLog($"请求URL: {PostUrl}");

            try
            {
                string jsonData = JsonConvert.SerializeObject(task);
                WriteLog($"序列化后的JSON: {jsonData}");

                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    WriteLog("开始发送POST请求...");
                    HttpResponseMessage response = await client.PostAsync(PostUrl, content);

                    if (response == null)
                    {
                        WriteLog("错误: HTTP响应为null");

                        // 记录响应为空错误
                        var responseErrorLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error",
                            Level = "Error",
                            Message = "HTTP响应为空",
                            Module = "ToTesApiService",
                            Operation = "SendAgvTaskAsync",
                            Details = $"请求URL: {PostUrl}, 请求参数: {jsonData}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(responseErrorLog);

                        return new ContainerReqsonInfo { returnCode = -1, returnMsg = "HTTP响应为空" };
                    }

                    WriteLog($"收到响应，状态码: {(int)response.StatusCode} {response.StatusCode}");

                    if (response.Content == null)
                    {
                        WriteLog("错误: 响应内容为null");

                        // 记录响应内容为空错误
                        var contentErrorLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error",
                            Level = "Error",
                            Message = "响应内容为空",
                            Module = "ToTesApiService",
                            Operation = "SendAgvTaskAsync",
                            Details = $"请求URL: {PostUrl}, 状态码: {(int)response.StatusCode}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(contentErrorLog);

                        return new ContainerReqsonInfo
                        {
                            returnCode = (int)response.StatusCode,
                            returnMsg = "响应内容为空"
                        };
                    }

                    string responseJson = await response.Content.ReadAsStringAsync();
                    WriteLog($"响应内容: {responseJson}");

                    // 记录HTTP响应
                    var httpResponseLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "HTTP Response",
                        Level = "Info",
                        Message = "查询托盘列表响应",
                        Module = "ToTesApiService",
                        Operation = "SendAgvTaskAsync",
                        Details = $"请求URL: {PostUrl}, 状态码: {(int)response.StatusCode}, 完整响应内容: {responseJson}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(httpResponseLog);

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var result = JsonConvert.DeserializeObject<ContainerReqsonInfo>(responseJson);

                            if (result == null)
                            {
                                WriteLog("错误: 反序列化结果为null");

                                // 记录反序列化错误
                                var deserializeErrorLog = new LogModel
                                {
                                    UserType = "http",
                                    LogType = "HTTP Error",
                                    Level = "Error",
                                    Message = "反序列化结果为空",
                                    Module = "ToTesApiService",
                                    Operation = "SendAgvTaskAsync",
                                    Details = $"请求URL: {PostUrl}, 响应内容: {responseJson}",
                                    UserId = "System",
                                    IpAddress = "System",
                                    CreateTime = DateTime.UtcNow,
                                    IsArchived = "False"
                                };
                                LogsDbContext.Insert(deserializeErrorLog);

                                return new ContainerReqsonInfo { returnCode = -1, returnMsg = "反序列化失败" };
                            }

                            WriteLog("反序列化响应成功");
                            WriteLog($"返回结果 - returnCode: {result.returnCode}, returnMsg: {result.returnMsg}");

                            // 详细记录data内容
                            if (result.data != null)
                            {
                                WriteLog($"Data内容 - count: {result.data.count}, curPageNum: {result.data.curPageNum}");

                                if (result.data.podList != null && result.data.podList.Any())
                                {
                                    WriteLog($"Pod列表数量: {result.data.podList.Count}");
                                    foreach (var pod in result.data.podList.Take(3)) // 只记录前3个pod
                                    {
                                        WriteLog($"Pod: {pod.podID}, 位置: {pod.curPosition}, 区域: {pod.regionCode}");
                                    }
                                    if (result.data.podList.Count > 3)
                                    {
                                        WriteLog($"... 还有 {result.data.podList.Count - 3} 个pod未显示");
                                    }
                                }
                                else
                                {
                                    WriteLog("Pod列表为空");
                                }
                            }
                            else
                            {
                                WriteLog("Data为null");
                            }

                            WriteLog("=== 请求成功完成 ===");

                            return result;
                        }
                        catch (JsonException jsonEx)
                        {
                            WriteLog($"响应JSON反序列化失败: {jsonEx.Message}");
                            WriteLog($"原始响应内容: {responseJson}");

                            // 记录JSON解析异常
                            var jsonErrorLog = new LogModel
                            {
                                UserType = "http",
                                LogType = "HTTP Error",
                                Level = "Error",
                                Message = "JSON解析错误",
                                Module = "ToTesApiService",
                                Operation = "SendAgvTaskAsync",
                                Details = $"请求URL: {PostUrl}, 异常信息: {jsonEx.Message}, 响应内容: {responseJson}",
                                UserId = "System",
                                IpAddress = "System",
                                CreateTime = DateTime.UtcNow,
                                IsArchived = "False"
                            };
                            LogsDbContext.Insert(jsonErrorLog);

                            return new ContainerReqsonInfo
                            {
                                returnCode = -1,
                                returnMsg = $"JSON解析错误: {jsonEx.Message}"
                            };
                        }
                    }
                    else
                    {
                        WriteLog($"请求失败，状态码: {(int)response.StatusCode} {response.StatusCode}");
                        WriteLog($"错误响应内容: {responseJson}");

                        // 记录HTTP错误响应
                        var httpErrorLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "查询托盘列表HTTP错误",
                            Module = "ToTesApiService",
                            Operation = "SendAgvTaskAsync",
                            Details = $"请求URL: {PostUrl}, 状态码: {(int)response.StatusCode}, 错误响应内容: {responseJson}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpErrorLog);

                        // 尝试解析错误响应
                        try
                        {
                            var errorResult = JsonConvert.DeserializeObject<ContainerReqsonInfo>(responseJson);
                            if (errorResult != null)
                            {
                                return errorResult;
                            }
                        }
                        catch
                        {
                            // 如果解析失败，返回原始错误信息
                        }

                        return new ContainerReqsonInfo
                        {
                            returnCode = (int)response.StatusCode,
                            returnMsg = $"请求失败: {response.StatusCode}",
                            returnUserMsg = responseJson
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"发生异常: {ex.Message}");
                WriteLog($"异常类型: {ex.GetType().FullName}");
                WriteLog($"异常堆栈: {ex.StackTrace}");

                // 记录异常信息
                var exceptionLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "查询托盘列表异常",
                    Module = "ToTesApiService",
                    Operation = "SendAgvTaskAsync",
                    Details = $"请求URL: {PostUrl}, 异常信息: {ex.Message}, 堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(exceptionLog);

                return new ContainerReqsonInfo
                {
                    returnCode = -1,
                    returnMsg = $"系统错误: {ex.Message}"
                };
            }
            finally
            {
                WriteLog("=== 请求处理结束 ===");
            }
        }

        /// <summary>
        /// 发送站点清除占用给WMS 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ReleaseStationsHttpResqon> PostPalnoRelease(ReleaseStationsHttp model)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "PostPalnoRelease_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            // 配置重试参数
            int maxRetries = 3; // 最大重试次数
            int retryDelayMs = 10000; // 重试延迟时间（毫秒）
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    // 记录HTTP请求 - 完整的请求信息
                    string serviceAddress = wmsapiip + "/api/WMS/PalnoRelease";
                    string requestJson = JsonConvert.SerializeObject(model);
                    var httpRequestLog = new LogModel
                    {
                        UserType = "http",
                        LogType = retryCount == 0 ? "HTTP Request" : $"HTTP Request Retry {retryCount}",
                        Level = "Info",
                        Message = retryCount == 0 ? "调用WMS API - 发送站点清除占用" : $"重试调用WMS API - 发送站点清除占用 (第{retryCount}次)",
                        Module = "ToTesApiService",
                        Operation = "PostPalnoRelease",
                        Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(httpRequestLog);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    string strJson = JsonConvert.SerializeObject(model);
                    WriteLog($"请求JSON: {strJson}");

                    // 写入请求体
                    using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                    {
                        dataStream.Write(strJson);
                        dataStream.Close();
                    }

                    WriteLog("请求发送成功，等待响应...");

                    // 发送请求并获取响应
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        string encoding = response.ContentEncoding;
                        if (string.IsNullOrEmpty(encoding))
                        {
                            encoding = "UTF-8"; //默认编码  
                        }

                        WriteLog($"收到响应，状态码: {(int)response.StatusCode} {response.StatusCode}");

                        if ((int)response.StatusCode == 200)
                        {
                            // 读取响应内容
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                            {
                                string retString = reader.ReadToEnd();
                                WriteLog($"响应内容: {retString}");

                                // 记录HTTP响应 - 完整的响应信息
                                var httpResponseLog = new LogModel
                                {
                                    UserType = "http",
                                    LogType = "HTTP Response",
                                    Level = "Info",
                                    Message = "WMS API响应 - 发送站点清除占用",
                                    Module = "ToTesApiService",
                                    Operation = "PostPalnoRelease",
                                    Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                                    UserId = "System",
                                    IpAddress = "System",
                                    CreateTime = DateTime.UtcNow,
                                    IsArchived = "False"
                                };
                                LogsDbContext.Insert(httpResponseLog);

                                var result = JObject.Parse(retString).ToObject<ReleaseStationsHttpResqon>();
                                return result;
                            }
                        }
                        else
                        {
                            // 非200状态码，准备重试
                            WriteLog($"收到非200状态码: {(int)response.StatusCode}，准备重试", true);

                            // 如果已达最大重试次数，则抛出异常
                            if (retryCount >= maxRetries)
                            {
                                throw new Exception($"达到最大重试次数({maxRetries})，最后一次状态码: {(int)response.StatusCode}");
                            }

                            // 等待指定时间后重试
                            retryCount++;
                            WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                            await Task.Delay(retryDelayMs);
                        }
                    }
                }
                catch (WebException webEx)
                {
                    // 处理网络异常
                    WriteLog($"网络请求异常: {webEx.Message}", true);

                    // 如果已达最大重试次数，则抛出异常
                    if (retryCount >= maxRetries)
                    {
                        throw new Exception($"达到最大重试次数({maxRetries})，最后错误: {webEx.Message}");
                    }

                    // 等待指定时间后重试
                    retryCount++;
                    WriteLog($"第{retryCount}次重试，{retryDelayMs / 1000}秒后执行...");
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception ex)
                {
                    // 其他异常直接抛出
                    WriteLog($"发生未预期异常: {ex.Message}", true);
                    throw;
                }
            }

            // 理论上不会执行到这里，因为要么成功返回，要么抛出异常
            throw new Exception("未知错误：重试逻辑异常");
        }

        /// <summary>
        /// 获取站点消息
        /// </summary>
        /// <returns></returns>
        public async Task GetStationMess(MessageType60 model)
        {
            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            void WriteLog(string message, bool isError = false)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "PostPalnoRelease_log.txt");
                    string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(isError ? "错误" : "信息")}: {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }
            // 或者保留时间戳+Guid
            string getWCSTaskID()
            {
                long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                string guidPart = Guid.NewGuid().ToString("N").Substring(0, 4);
                return $"WCS{timestamp}{guidPart}";
            }
            if (model.occupyStatus == 3)
            {
                Stationstatus stationstatus = StationstatusCollection.DataList.FirstOrDefault(x => x.StationCode == model.stationCode);
                if (stationstatus == null) return;
                ConveSendMessCollection.Add(new ConveSendMess
                {
                    MessType = "CL",
                    PLCIP = "172.18.18.20",
                    FromLocation = stationstatus.TesCheckName,
                    ToLocation = stationstatus.TesCheckName,
                    StUintID = model.podID,
                    StUnitHeight = "",
                    StUnitWeight = "",
                    CanWrite = "01",
                });
            }
            //else if (model.occupyStatus == 2)
            //{
            //    Stationstatus stationstatus = StationstatusCollection.DataList.FirstOrDefault(x => x.StationCode == model.stationCode);
            //    if (stationstatus == null) return;
            //    string tolocation = "";
            //    if (stationstatus.TesCheckName == "6006")
            //    {
            //        tolocation = "6004";
            //    }
            //    else if (stationstatus.TesCheckName == "6003")
            //    {
            //        tolocation = "6002";
            //    }
            //    ConveSendMessCollection.Add(new ConveSendMess
            //    {
            //        MessType = "TO",
            //        PLCIP = "172.18.18.20",
            //        FromLocation = stationstatus.TesCheckName,
            //        ToLocation = tolocation,
            //        StUintID = model.podID,
            //        StUnitHeight = "",
            //        StUnitWeight = "",
            //        CanWrite = "01",
            //    });
            //}
        }

        /// <summary>
        /// 修改任务优先级
        /// </summary>
        /// <param name="updateTaskPriority"></param>
        /// <returns></returns>
        public static ToWcsPublicReturnValueL updateTaskPriority(UpdateTaskPriorityModel updateTaskPriority)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/updateTaskPriority";

                // 记录HTTP请求
                UpdateTaskPriorityModel taskmodel2 = new UpdateTaskPriorityModel();
                taskmodel2 = updateTaskPriority;
                taskmodel2.taskID = updateTaskPriority.taskID;
                taskmodel2.RequestID = updateTaskPriority.taskID.ToString();

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES修改任务优先级API",
                    Module = "ToTesApiService",
                    Operation = "updateTaskPriority",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES修改任务优先级API响应",
                            Module = "ToTesApiService",
                            Operation = "updateTaskPriority",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson parm = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        ToWcsPublicReturnValueL result = new ToWcsPublicReturnValueL(parm.returnCode, parm.returnMsg, parm.returnUserMsg, parm.data);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES修改任务优先级API调用异常",
                    Module = "ToTesApiService",
                    Operation = "updateTaskPriority",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES修改任务优先级API错误响应",
                            Module = "ToTesApiService",
                            Operation = "updateTaskPriority",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ToWcsPublicReturnValueL>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return ToWcsPublicReturnValueL.Error($"HTTP错误: {errorResponse.StatusCode}");
                        }
                    }
                }

                // 其他类型的异常
                return ToWcsPublicReturnValueL.Error("网络请求异常");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES修改任务优先级API系统异常",
                    Module = "ToTesApiService",
                    Operation = "updateTaskPriority",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return ToWcsPublicReturnValueL.Error("系统异常");
            }
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="pauseTasks"></param>
        /// <returns></returns>
        public static ToWcsPublicReturnValueL pauseTasks(PauseTasksModel pauseTasks)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/pauseTasks";

                // 记录HTTP请求
                PauseTasksModel taskmodel2 = new PauseTasksModel();
                taskmodel2 = pauseTasks;
                taskmodel2.taskIDs = pauseTasks.taskIDs;
                taskmodel2.RequestID = pauseTasks.taskIDs.ToString();

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES暂停任务API",
                    Module = "ToTesApiService",
                    Operation = "pauseTasks",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES暂停任务API响应",
                            Module = "ToTesApiService",
                            Operation = "pauseTasks",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson parm = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        ToWcsPublicReturnValueL result = new ToWcsPublicReturnValueL(parm.returnCode, parm.returnMsg, parm.returnUserMsg, parm.data);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES暂停任务API调用异常",
                    Module = "ToTesApiService",
                    Operation = "pauseTasks",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES暂停任务API错误响应",
                            Module = "ToTesApiService",
                            Operation = "pauseTasks",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ToWcsPublicReturnValueL>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return ToWcsPublicReturnValueL.Error($"HTTP错误: {errorResponse.StatusCode}");
                        }
                    }
                }

                // 其他类型的异常
                return ToWcsPublicReturnValueL.Error("网络请求异常");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES暂停任务API系统异常",
                    Module = "ToTesApiService",
                    Operation = "pauseTasks",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return ToWcsPublicReturnValueL.Error("系统异常");
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="resumeTasks"></param>
        /// <returns></returns>
        public static ToWcsPublicReturnValueL resumeTasks(PauseTasksModel resumeTasks)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/resumeTasks";

                // 记录HTTP请求
                PauseTasksModel taskmodel2 = new PauseTasksModel();
                taskmodel2 = resumeTasks;
                taskmodel2.taskIDs = resumeTasks.taskIDs;
                taskmodel2.RequestID = resumeTasks.taskIDs.ToString();

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES暂停任务API",
                    Module = "ToTesApiService",
                    Operation = "resumeTasks",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES暂停任务API响应",
                            Module = "ToTesApiService",
                            Operation = "resumeTasks",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson parm = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        ToWcsPublicReturnValueL result = new ToWcsPublicReturnValueL(parm.returnCode, parm.returnMsg, parm.returnUserMsg, parm.data);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES暂停任务API调用异常",
                    Module = "ToTesApiService",
                    Operation = "resumeTasks",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES暂停任务API错误响应",
                            Module = "ToTesApiService",
                            Operation = "resumeTasks",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ToWcsPublicReturnValueL>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return ToWcsPublicReturnValueL.Error($"HTTP错误: {errorResponse.StatusCode}");
                        }
                    }
                }

                // 其他类型的异常
                return ToWcsPublicReturnValueL.Error("网络请求异常");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES暂停任务API系统异常",
                    Module = "ToTesApiService",
                    Operation = "resumeTasks",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return ToWcsPublicReturnValueL.Error("系统异常");
            }
        }

        /// <summary>
        /// 重试任务
        /// </summary>
        /// <param name="retryTask"></param>
        /// <returns></returns>
        public static ToWcsPublicReturnValueL retryTask(TESretryTaskModel testaskid)
        {
            try
            {
                string serviceAddress = tesapiip + "/tes/apiv2/retryTask";

                // 记录HTTP请求
                TESretryTaskModel taskmodel2 = testaskid;

                string requestJson = JsonConvert.SerializeObject(taskmodel2);
                var httpRequestLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Request",
                    Level = "Info",
                    Message = "调用TES重试任务API",
                    Module = "ToTesApiService",
                    Operation = "cancelTask",
                    Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(httpRequestLog);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "POST";
                request.ContentType = "application/json";

                string strJson = JsonConvert.SerializeObject(taskmodel2);

                // 写入请求体
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strJson);
                }

                // 获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string encoding = response.ContentEncoding;
                    if (string.IsNullOrEmpty(encoding))
                    {
                        encoding = "UTF-8";
                    }

                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                    {
                        string retString = reader.ReadToEnd();

                        // 记录HTTP响应
                        var httpResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Response",
                            Level = "Info",
                            Message = "TES重试任务API响应",
                            Module = "ToTesApiService",
                            Operation = "cancelTask",
                            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(httpResponseLog);

                        // 将JSON字符串反序列化为ContainerShelvingReqson对象
                        ContainerShelvingReqson parm = JsonConvert.DeserializeObject<ContainerShelvingReqson>(retString);
                        ToWcsPublicReturnValueL result = new ToWcsPublicReturnValueL(parm.returnCode, parm.returnMsg, parm.returnUserMsg, parm.data);
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                // 记录HTTP异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES重试任务API调用异常",
                    Module = "ToTesApiService",
                    Operation = "cancelTask",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理HTTP错误
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (Stream stream = errorResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string errorContent = reader.ReadToEnd();

                        // 记录HTTP错误响应
                        var errorResponseLog = new LogModel
                        {
                            UserType = "http",
                            LogType = "HTTP Error Response",
                            Level = "Error",
                            Message = "TES重试任务API错误响应",
                            Module = "ToTesApiService",
                            Operation = "cancelTask",
                            Details = $"HTTP状态码: {(int)errorResponse.StatusCode}\n错误响应内容: {errorContent}",
                            UserId = "System",
                            IpAddress = "System",
                            CreateTime = DateTime.UtcNow,
                            IsArchived = "False"
                        };
                        LogsDbContext.Insert(errorResponseLog);

                        // 尝试解析错误响应
                        try
                        {
                            return JsonConvert.DeserializeObject<ToWcsPublicReturnValueL>(errorContent);
                        }
                        catch
                        {
                            // 如果无法解析为JSON，返回一个包含错误信息的对象
                            return ToWcsPublicReturnValueL.Error($"HTTP错误: {errorResponse.StatusCode}");
                        }
                    }
                }

                // 其他类型的异常
                return ToWcsPublicReturnValueL.Error("网络请求异常");
            }
            catch (Exception ex)
            {
                // 记录其他异常
                var errorLog = new LogModel
                {
                    UserType = "http",
                    LogType = "HTTP Error",
                    Level = "Error",
                    Message = "TES重试任务API系统异常",
                    Module = "ToTesApiService",
                    Operation = "cancelTask",
                    Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                    UserId = "System",
                    IpAddress = "System",
                    CreateTime = DateTime.UtcNow,
                    IsArchived = "False"
                };
                LogsDbContext.Insert(errorLog);

                // 处理其他异常
                return ToWcsPublicReturnValueL.Error("系统异常");
            }
        }
    }
}
