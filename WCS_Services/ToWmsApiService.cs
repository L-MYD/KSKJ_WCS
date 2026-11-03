using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCS_Helper; // nan_T 2026-09-09：日志统一走 LogHelper（落库），不再写 txt
using WCS_Helper.Mapper;
using WCS_Models.SqlModel;
using WCS_Models.TESModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Models.WCSModel;
using WCS_Models.WMSModel;

using WCS_IServices;
namespace WCS_Services
{
    public class ToWmsApiService : IToWmsApiService
    {
        /// <summary>
        /// 创建搬运任务（按《WMS-WCS对接协议》/api/WCS/WMSTaskDis 的任务类型处理）
        /// </summary>
        public WcsReturn newMoveTask(WMSTaskDisModel model)
        {
            // 记录HTTP请求开始
            var startLog = new LogModel
            {
                UserType = "http",
                LogType = "HTTP Request",
                Level = "Info",
                Message = "开始处理容器搬运任务",
                Module = "ToWmsApiService",
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
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                    // nan_T 2026-09-09：不再写 newMoveTask_log.txt，API 日志统一落 logs 表（来源=API/接口日志）
                    LogHelper.Api("newMoveTask", logMessage);
                    Console.WriteLine(logMessage);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"写入日志失败: {logEx.Message}");
                }
            }

            try
            {
                // 检查是否已存在相同的任务（使用数据库唯一约束更可靠）
                TescorrespondsWms tescorrespondsWms = TaskDbContext.GetByWmsTaskIdAsync(model.WmsId);
                if (tescorrespondsWms != null)
                {
                    if (tescorrespondsWms.Podid == model.Palno && tescorrespondsWms.CurrentPosition == model.FromLocation && tescorrespondsWms.TargetLocation == model.ToLocation)
                    {
                        WriteLog($"检测到重复任务: {model.WmsId}, 容器号: {model.Palno}");
                        return new WcsReturn { msgType = "F", msgText = "WMS当前任务号已有成功任务", WmsId = model.WmsId };
                    }
                }

                WriteLog("=== 开始处理容器搬运任务 ===");
                WriteLog($"容器号: {model.Palno}, 任务ID: {model.WmsId}, 任务类型: {model.TaskType}");

                DesExt desext = new DesExt();
                TaskExt taskExt = new TaskExt();

                string fromlococation = model.FromLocation,
                    tolococation = model.ToLocation;
                string tasktype = "";

                // 起始地映射：优先按站台（口地址）解析，其次按货位解析，最后保留 WMS 原始编码
                StationNameModel fromstationname = StationNameModelCollection.DataList.FirstOrDefault(x => x.WmsStationName == model.FromLocation);
                Storagelocation fromstoragelocation = StoragelocationCollection.DataList.FirstOrDefault(x => x.Wmsreference == model.FromLocation);
                if (fromstationname != null)
                {
                    fromlococation = fromstationname.TesStationName;
                }
                else if (fromstoragelocation != null)
                {
                    fromlococation = fromstoragelocation.Location;
                }
                // nan_T 2026-10-30：字典缺失时保留模型原始编码，避免空引用

                // 目的地映射：命中站台字典即为站台式任务（出库类），否则按货位解析（入库类）
                StationNameModel tostationname = StationNameModelCollection.DataList.FirstOrDefault(x => x.WmsStationName == model.ToLocation);
                Aisles toaisles = AislesCollection.DataList.FirstOrDefault(x => x.WmsReference == model.ToLocation);
                Storagelocation tostoragelocation = StoragelocationCollection.DataList.FirstOrDefault(x => x.Wmsreference == model.ToLocation);

                bool isOutTask;
                if (tostationname != null)
                {
                    tolococation = tostationname.TesStationName;
                    isOutTask = true;
                }
                else if (toaisles != null)
                {
                    tolococation = toaisles.AislesName;
                    isOutTask = false;
                }
                else if (tostoragelocation != null)
                {
                    tolococation = tostoragelocation.Location;
                    isOutTask = false;
                }
                else
                {
                    // nan_T 2026-10-30：字典未命中时按协议任务类型判断方向
                    tolococation = model.ToLocation;
                    isOutTask = IsOutTaskType(model.TaskType);
                }

                if (isOutTask)
                {
                    desext.Unload = 1;
                    taskExt.AutoToRest = 1;
                    tasktype = "出库任务";
                }
                else
                {
                    desext.Unload = 0;
                    taskExt.AutoToRest = 0;
                    tasktype = "入库任务";
                }

                WriteLog($"当前目标位置: {model.ToLocation}, 当前TES目标位置{tolococation},当前WMS初始位置: {model.FromLocation},当前TES初始位置{fromlococation}, 任务ID: {model.WmsId}");

                // 出库类任务需检查目标站台是否被占用，被占用则不创建任务
                if (isOutTask)
                {
                    Stationstatus stationstatus = StationstatusCollection.DataList.FirstOrDefault(x => x.StationCode == tolococation);
                    // nan_T 2026-10-30：字典缺失时按未占用处理，避免空引用
                    if (stationstatus != null && stationstatus.Status == "占用")
                    {
                        WriteLog($"目标站台 {tolococation} 当前状态为占用，无法创建搬运任务");
                        WMSTaskDis wMSTaskDis = new WMSTaskDis
                        {
                            WmsId = model.WmsId,
                            WcsId = getWCSTaskID(),
                            FromLocation = fromlococation,
                            ToLocation = tolococation,
                            Palno = model.Palno,
                            IfPicking = model.IfPicking,
                            Num = model.Num,
                            TaskType = tasktype,
                        };
                        // nan_T 2026-10-30：写入 WMSTaskDis 临时表，供后续调度使用
                        int reinsert = WcsSQLServer.ExecuteInsert("WMSTaskDis", wMSTaskDis);
                        WriteLog($"数据库插入结果: {reinsert} 条记录受影响 , 容器号为：{wMSTaskDis.Palno}");
                        return new WcsReturn { msgType = "S", msgText = "成功", WmsId = model.WmsId };
                    }
                }

                // 组装 TES 创建任务参数（TES 侧模型保持不变）
                var tesModel = new newMoveTaskModel
                {
                    wmsId = model.WmsId,
                    podId = model.Palno,
                    currentPosition = fromlococation,
                    desPosition = tolococation,
                    wmsTaskMessType = model.TaskType,
                    priority = int.TryParse(model.Num, out var priority) ? priority : 0
                };

                TaskReqsonModel trm = ToTesApiService.CreateMoveTask(tesModel, desext, taskExt); // 创建搬运任务接口
                if (trm.returnUserMsg.Equals("成功"))
                {
                    WriteLog("搬运任务创建成功，保存到数据库...");

                    string wcsId = getWCSTaskID();
                    TescorrespondsWms tescorresponds = new TescorrespondsWms
                    {
                        TesTaskID = trm.data.taskID.ToString(),
                        WmsTaskID = model.WmsId,
                        // nan_T 2026-10-30：按协议任务类型记录（01入成品库/02入灭菌缓存/03灭菌/04入解析区/05PS/06AGV）
                        WmsTaskType = GetTaskTypeName(model.TaskType),
                        CurrentPosition = fromlococation,
                        Statu = "待执行",
                        CreationTime = DateTime.UtcNow,
                        CompletionTime = DateTime.UtcNow,
                        TargetLocation = tolococation,
                        Podid = model.Palno,
                        WcsId = wcsId,
                    };

                    int reinsert = TaskDbContext.InsertDataAsync(tescorresponds);
                    WriteLog($"数据库插入结果: {reinsert} 条记录受影响 , 任务状态为：{tescorresponds.Statu}");

                    // Pallet-Check 站台创建任务后置为占用
                    if (tescorresponds.TargetLocation == "Pallet-Check-F1" || tescorresponds.TargetLocation == "Pallet-Check-F5")
                    {
                        Stationstatus station = StationstatusCollection.DataList.FirstOrDefault(x => x.StationCode == tescorresponds.TargetLocation);
                        // nan_T 2026-10-30：字典缺失时跳过占用回写，避免空引用
                        if (station != null)
                        {
                            station.Status = "占用";
                            TaskDbContext.UpdateStationstatusAsync(station);
                        }
                    }

                    var successLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "Business Success",
                        Level = "Info",
                        Message = "容器搬运任务创建成功",
                        Module = "ToWmsApiService",
                        Operation = "newMoveTask",
                        Details = $"任务ID: {model.WmsId}, 容器号: {model.Palno}, 数据库插入结果: {reinsert}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(successLog);

                    WriteLog("=== 任务处理成功 ===");
                    return new WcsReturn { msgType = "S", msgText = "成功", WmsId = model.WmsId };
                }
                else
                {
                    WriteLog($"创建搬运任务失败: {trm?.returnUserMsg}");

                    var failureLog = new LogModel
                    {
                        UserType = "http",
                        LogType = "Business Error",
                        Level = "Error",
                        Message = "创建搬运任务失败",
                        Module = "ToWmsApiService",
                        Operation = "newMoveTask",
                        Details = $"任务ID: {model.WmsId}, 容器号: {model.Palno}, 错误信息: {trm?.returnUserMsg}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(failureLog);

                    return new WcsReturn { msgType = "F", msgText = trm?.returnUserMsg, WmsId = model.WmsId };
                }
            }
            catch (Exception ex)
            {
                return new WcsReturn { msgType = "E", msgText = ex.Message, WmsId = model.WmsId };
            }
        }

        /// <summary>
        /// 协议任务类型名称
        /// 01：入成品库任务 02：入灭菌缓存任务 03：灭菌任务 04：入解析区任务 05：无任务组（PS 任务）06：无任务组（AGV 任务）
        /// </summary>
        private static string GetTaskTypeName(string taskType)
        {
            switch (taskType)
            {
                case "01": return "入成品库任务";
                case "02": return "入灭菌缓存任务";
                case "03": return "灭菌任务";
                case "04": return "入解析区任务";
                case "05": return "无任务组（PS任务）";
                case "06": return "无任务组（AGV任务）";
                default: return taskType;
            }
        }

        /// <summary>
        /// 字典未命中时按任务类型判断是否为出库类任务（03 灭菌、05 PS、06 AGV 为站台式任务）
        /// </summary>
        private static bool IsOutTaskType(string taskType)
        {
            return taskType == "03" || taskType == "05" || taskType == "06";
        }
    }
}
