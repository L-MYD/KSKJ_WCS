using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCS_Helper.Mapper;
using WCS_Models.SqlModel;
using WCS_Models.TESModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Models.WCSModel;
using WCS_Models.WMSModel;
using WCS_Models.PLCModel;

using WCS_IServices;
namespace WCS_Services
{
    public class ToWmsApiService : IToWmsApiService
    {
        // 注意：此文件中只有 newMoveTask 方法，且为同步方法，保持原样
        // receiveTask 方法在 ToTesApiService 中，此处不重复

        /// <summary>
        /// 创建出库/拣选任务（同步方法，保持原有逻辑）
        /// </summary>
        public WcsReturn newMoveTask(newMoveTaskModel model)
        {
            // 记录HTTP请求开始
            var startLog = new LogModel
            {
                UserType = "http",
                LogType = "HTTP Request",
                Level = "Info",
                Message = "开始处理容器搬运任务",
                Module = "ToTesApiService",
                Operation = "newMoveTask",
                Details = $"任务参数: {JsonConvert.SerializeObject(model)}",
                UserId = "System",
                IpAddress = "System",
                CreateTime = DateTime.UtcNow,
                IsArchived = "False"
            };
            LogsDbContext.Insert(startLog);

            // 初始化日志路径
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            string getWCSTaskID()
            {
                long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                string guidPart = Guid.NewGuid().ToString("N").Substring(0, 4);
                return $"WCS{timestamp}{guidPart}";
            }

            void WriteLog(string message)
            {
                try
                {
                    string logFilePath = Path.Combine(logPath, "newMoveTask_log.txt");
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
                // 检查重复任务
                TescorrespondsWms tescorrespondsWms = TaskDbContext.GetByWmsTaskIdAsync(model.wmsId);
                if (tescorrespondsWms != null)
                {
                    if (tescorrespondsWms.Podid == model.podId && tescorrespondsWms.CurrentPosition == model.currentPosition && tescorrespondsWms.TargetLocation == model.desPosition)
                    {
                        WriteLog($"检测到重复任务: {model.wmsId}, 容器号: {model.podId}");
                        return new WcsReturn { returnCode = 0, returnMsg = "WMS当前任务号已有成功任务" };
                    }
                }

                WriteLog("=== 开始处理容器搬运任务 ===");
                WriteLog($"容器号: {model.podId}, 任务ID: {model.wmsId}");
                DesExt desext = new DesExt();
                TaskExt taskExt = new TaskExt();
                desext.Unload = 1;
                taskExt.AutoToRest = 1;

                string fromlococation = model.currentPosition,
                    tolococation = model.desPosition;
                string tasktype = "";

                if (model.wmsTaskMessType == "OUT" || model.wmsTaskMessType == "PD") // 出库任务
                {
                    switch (model.desPosition)
                    {
                        case "DPJ-01":
                            tolococation = "P-OUT01";
                            model.wmsTaskMessType = "FOLD";
                            break;
                        case "DPJ-02":
                            tolococation = "P-OUT02";
                            model.wmsTaskMessType = "FOLD";
                            break;
                        case "DPJ-03":
                            tolococation = "P-OUT03";
                            model.wmsTaskMessType = "FOLD";
                            break;
                    }
                    Storagelocation fromstoragelocation = StoragelocationCollection.DataList.FirstOrDefault(x => x.Wmsreference == model.currentPosition);
                    Stationstatus tostationname = StationstatusCollection.DataList.FirstOrDefault(x => x.WmsStationName == tolococation);
                    fromlococation = fromstoragelocation.Location;
                    tolococation = tostationname.StationCode;
                    tasktype = model.wmsTaskMessType;
                }
                else if (model.wmsTaskMessType == "IN" || model.wmsTaskMessType == "WH") // 回库/移库
                {
                    Storagelocation tostoragelocation = StoragelocationCollection.DataList.FirstOrDefault(x => x.Wmsreference == model.desPosition);
                    Stationstatus fromstationname = StationstatusCollection.DataList.FirstOrDefault(x => x.WmsStationName == model.currentPosition);
                    if (fromstationname == null)
                    {
                        Storagelocation fromstoragelocation = StoragelocationCollection.DataList.FirstOrDefault(x => x.Wmsreference == model.currentPosition);
                        fromlococation = fromstoragelocation.Location;
                    }
                    else
                    {
                        fromlococation = fromstationname.TesCheckName;
                    }
                    tolococation = tostoragelocation.Location;
                    desext.Unload = 1;
                    taskExt.AutoToRest = 1;
                    tasktype = model.wmsTaskMessType;
                }

                WriteLog($"当前目标位置: {model.desPosition}, 当前TES目标位置{tolococation},当前WMS初始位置: {model.currentPosition},当前TES初始位置{fromlococation}, 任务ID: {model.wmsId}");
                model.desPosition = tolococation;
                model.currentPosition = fromlococation;

                // 调用TES创建任务
                TaskReqsonModel trm = ToTesApiService.CreateMoveTask(model, desext, taskExt);

                if (trm.returnUserMsg.Equals("成功"))
                {
                    WriteLog("搬运任务创建成功，保存到数据库...");

                    TescorrespondsWms tescorresponds = new TescorrespondsWms
                    {
                        TesTaskID = trm.data.taskID.ToString(),
                        WmsTaskID = model.wmsId,
                        WmsTaskType = tasktype,
                        CurrentPosition = fromlococation,
                        Statu = "待执行",
                        CreationTime = DateTime.UtcNow,
                        CompletionTime = DateTime.UtcNow,
                        TargetLocation = tolococation,
                        Podid = model.podId,
                        WcsId = getWCSTaskID(),
                    };

                    int reinsert = TaskDbContext.InsertDataAsync(tescorresponds);
                    WriteLog($"数据库插入结果: {reinsert} 条记录受影响 , 任务状态为：{tescorresponds.Statu}");

                    var taskMessage = new WcsReturn
                    {
                        returnCode = trm.returnCode,
                        returnMsg = trm.returnMsg
                    };

                    var successLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "Business Success",
                        Level = "Info",
                        Message = "容器搬运任务创建成功",
                        Module = "ToTesApiService",
                        Operation = "newMoveTask",
                        Details = $"任务ID: {model.wmsId}, 容器号: {model.podId}, 数据库插入结果: {reinsert}, 完整响应: {JsonConvert.SerializeObject(taskMessage)}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(successLog);

                    WriteLog("=== 任务处理成功 ===");
                    return taskMessage;
                }
                else
                {
                    WriteLog($"创建搬运任务失败: {trm?.returnUserMsg}");

                    var taskMessage = new WcsReturn
                    {
                        returnCode = trm.returnCode,
                        returnMsg = trm.returnMsg
                    };

                    var failureLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "Business Error",
                        Level = "Error",
                        Message = "创建搬运任务失败",
                        Module = "ToTesApiService",
                        Operation = "newMoveTask",
                        Details = $"任务ID: {model.wmsId}, 容器号: {model.podId}, 错误信息: {trm?.returnUserMsg}, 完整响应: {JsonConvert.SerializeObject(taskMessage)}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(failureLog);

                    return taskMessage;
                }
            }
            catch (Exception ex)
            {
                return new WcsReturn { returnCode = -1, returnMsg = ex.Message };
            }
        }
    }
}