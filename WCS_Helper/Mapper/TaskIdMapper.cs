//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using WCS_Models.LoginViewModel;
//using WCS_Models.PLCModel;
//using WCS_Models.SqlModel;
//using WCS_Models.WCSModel;
//using WCS_Models.WCSModel.LogModel;
//using WCS_Models.WMSModel;

//namespace WCS_Helper.Mapper
//{
//    public static class TaskIdMapper
//    {
//        #region 辅助方法
//        private static WcsDbContext CreateDbContext()
//        {
//            return new WcsDbContext();
//        }

//        private static void LogError(string message, string details, Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string operation = "")
//        {
//            var errorLog = new LogModel
//            {
//                UserType = "database",
//                LogType = "Database Error",
//                Level = "Error",
//                Message = message,
//                Module = "TaskIdMapper",
//                Operation = operation,
//                Details = $"{details}\n异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
//                UserId = "System",
//                IpAddress = "System",
//                CreateTime = DateTime.UtcNow,
//                IsArchived = "False"
//            };
//            LogsDbContext.Insert(errorLog);
//        }

//        private static void LogInfo(string message, string details, [System.Runtime.CompilerServices.CallerMemberName] string operation = "")
//        {
//            var infoLog = new LogModel
//            {
//                UserType = "database",
//                LogType = "Database Info",
//                Level = "Info",
//                Message = message,
//                Module = "TaskIdMapper",
//                Operation = operation,
//                Details = details,
//                UserId = "System",
//                IpAddress = "System",
//                CreateTime = DateTime.UtcNow,
//                IsArchived = "False"
//            };
//            LogsDbContext.Insert(infoLog);
//        }

//        public static void SaveErrToText(string detail, string reason)
//        {
//            // 保持原文件日志方法不变（或可调用优化后的 Logger）
//            const string FilePath = @"C:\WCSErrLog";
//            var fileName = DateTime.UtcNow.ToString("yyyyMMdd");
//            StreamWriter sw = null;
//            FileStream fs = null;
//            try
//            {
//                if (!Directory.Exists(FilePath))
//                    Directory.CreateDirectory(FilePath);
//                fileName = "WcsErrLog_" + fileName + ".txt";
//                var fullPath = Path.Combine(FilePath, fileName);
//                if (!File.Exists(fullPath))
//                {
//                    fs = File.Create(fullPath);
//                    fs.Close();
//                    fs.Dispose();
//                }
//                sw = File.AppendText(fullPath);
//                var addTxt = $"DealMessage:{detail} \n Reason:{reason} \n Datetime:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} \n";
//                sw.Write(addTxt);
//                sw.Close();
//            }
//            catch
//            {
//                sw?.Close();
//                fs?.Close();
//            }
//        }
//        #endregion

//        #region 查询方法
//        public static TescorrespondsWms QueryDataAsync(TescorrespondsWms tcw)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.TescorrespondsWms.FirstOrDefault(t => t.id == tcw.id);
//            }
//            catch (Exception ex)
//            {
//                LogError("查询TescorrespondsWms表失败", $"id={tcw.id}", ex);
//                SaveErrToText("查询数据时发生错误", ex.Message);
//                return new TescorrespondsWms();
//            }
//        }

//        public static List<T> SelectContent<T>(string TableName) where T : class
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.Set<T>().ToList();
//            }
//            catch (Exception ex)
//            {
//                LogError($"查询{TableName}表失败", $"TableName={TableName}", ex);
//                SaveErrToText($"查询{TableName}表失败", ex.Message);
//                return new List<T>();
//            }
//        }

//        public static TescorrespondsWms GetByTesTaskIdAsync(string tesTaskId)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.TescorrespondsWms.FirstOrDefault(t => t.TesTaskID == tesTaskId);
//            }
//            catch (Exception ex)
//            {
//                LogError("根据TesTaskID查询数据异常", $"TesTaskID={tesTaskId}", ex);
//                SaveErrToText("根据TesTaskID查询失败", ex.Message);
//                return null;
//            }
//        }

//        public static WCSAddPodModel GetAddPodModel(string podid)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.AddPod.FirstOrDefault(a => a.PodID == podid);
//            }
//            catch (Exception ex)
//            {
//                LogError("根据Podid查询数据异常", $"Podid={podid}", ex);
//                SaveErrToText("根据Podid查询失败", ex.Message);
//                return null;
//            }
//        }

//        public static TescorrespondsWms GetByTescontainerNoAsync(string containerNo)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.TescorrespondsWms
//                    .Where(t => t.Podid == containerNo)
//                    .OrderByDescending(t => t.id)
//                    .FirstOrDefault();
//            }
//            catch (Exception ex)
//            {
//                LogError("根据ContainerNo查询数据异常", $"ContainerNo={containerNo}", ex);
//                SaveErrToText("根据ContainerNo查询失败", ex.Message);
//                return null;
//            }
//        }

//        public static List<TescorrespondsWms> GetAllPendingTasksFromDatabase(string TargetLocation)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.TescorrespondsWms
//                    .Where(t => t.Statu != "输送机出库完成"
//                                && t.WmsTaskType == "出库任务"
//                                && t.TargetLocation == TargetLocation)
//                    .ToList();
//            }
//            catch (Exception ex)
//            {
//                LogError("获取待执行任务失败", $"TargetLocation={TargetLocation}", ex);
//                SaveErrToText("查询待执行任务失败", ex.Message);
//                return new List<TescorrespondsWms>();
//            }
//        }

//        public static TescorrespondsWms GetByTargetLocationAsync(string TargetLocation, string Podid)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var result = context.TescorrespondsWms
//                    .Where(t => t.TargetLocation == TargetLocation
//                                && t.Podid == Podid
//                                && t.WmsTaskType == "出库任务")
//                    .OrderByDescending(t => t.id)
//                    .FirstOrDefault();
//                LogInfo("根据CurrentPosition查询结果", $"TargetLocation={TargetLocation}, Podid={Podid}, Result={JsonConvert.SerializeObject(result)}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("根据CurrentPosition查询数据异常", $"TargetLocation={TargetLocation}, Podid={Podid}", ex);
//                SaveErrToText("根据CurrentPosition查询失败", ex.Message);
//                return null;
//            }
//        }

//        public static TescorrespondsWms GetByWmsTaskIdAsync(string wmsTaskId)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.TescorrespondsWms.FirstOrDefault(t => t.WmsTaskID == wmsTaskId);
//            }
//            catch (Exception ex)
//            {
//                LogError("根据WmsTaskID查询数据异常", $"WmsTaskID={wmsTaskId}", ex);
//                SaveErrToText("根据WmsTaskID查询失败", ex.Message);
//                return null;
//            }
//        }

//        public static Aisles GetByWmsReferenceAsync(string WmsReference)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.Aisles.FirstOrDefault(a => a.WmsReference == WmsReference);
//            }
//            catch (Exception ex)
//            {
//                LogError("根据WmsReference查询数据异常", $"WmsReference={WmsReference}", ex);
//                SaveErrToText("根据WmsReference查询失败", ex.Message);
//                return null;
//            }
//        }

//        public static StationCodeWithNum GetStationCodeWithNumByCode(string code)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var result = context.StationCodeWithNum.FirstOrDefault(s => s.code == code);
//                LogInfo("根据站点名称查询站点编号", $"code={code}, Result={JsonConvert.SerializeObject(result)}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("根据站点名称查询站点编号异常", $"code={code}", ex);
//                SaveErrToText("根据站点名称查询站点编号失败", ex.Message);
//                return null;
//            }
//        }

//        public static TescorrespondsWms GetTaskNumWithTaskType(string TargetLocation)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                return context.TescorrespondsWms
//                    .Where(t => t.TargetLocation == TargetLocation
//                                && t.Statu != "到达目的地"
//                                && t.Statu != "任务失败"
//                                && t.Statu != "托盘空取"
//                                && t.Statu != "任务已取消")
//                    .OrderByDescending(t => t.CreationTime)
//                    .FirstOrDefault();
//            }
//            catch (Exception ex)
//            {
//                LogError("根据任务类型查询任务数量", $"TargetLocation={TargetLocation}", ex);
//                SaveErrToText("根据任务类型查询任务数量失败", ex.Message);
//                return null;
//            }
//        }

//        public static StationCodeWithNum GetStationCodeWithNumByNum(string nodes_uni)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var result = context.StationCodeWithNum.FirstOrDefault(s => s.nodes_uni == nodes_uni);
//                LogInfo("根据站点编号查询站点名称", $"nodes_uni={nodes_uni}, Result={JsonConvert.SerializeObject(result)}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("根据站点编号查询站点名称异常", $"nodes_uni={nodes_uni}", ex);
//                SaveErrToText("根据站点编号查询站点名称失败", ex.Message);
//                return null;
//            }
//        }

//        public static TescorrespondsWms GetByTesstationCodeAsync(string stationCode, string statu, string podid)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var result = context.TescorrespondsWms
//                    .FirstOrDefault(t => t.CurrentPosition == stationCode
//                                         && t.Statu == statu
//                                         && t.Podid == podid);
//                LogInfo("根据站点、状态和容器号查询", $"stationCode={stationCode}, statu={statu}, podid={podid}, Result={JsonConvert.SerializeObject(result)}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("根据站点、状态和容器号查询异常", $"stationCode={stationCode}, statu={statu}, podid={podid}", ex);
//                SaveErrToText("根据站点、状态和容器号查询失败", ex.Message);
//                return null;
//            }
//        }
//        #endregion

//        #region 插入方法
//        public static int InsertDataAsync(TescorrespondsWms tcm)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                context.TescorrespondsWms.Add(tcm);
//                int result = context.SaveChanges();
//                LogInfo("插入TescorrespondsWms", $"Data={JsonConvert.SerializeObject(tcm)}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("向TescorrespondsWms表插入数据异常", $"Data={JsonConvert.SerializeObject(tcm)}", ex);
//                SaveErrToText("添加数据时发生错误", ex.Message);
//                return 0;
//            }
//        }

//        public static int InsertWMSTaskDis(WMSTaskDis task)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                context.WMSTaskDis.Add(task);
//                int result = context.SaveChanges();
//                LogInfo("插入WMSTaskDis", $"Data={JsonConvert.SerializeObject(task)}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("向WMSTaskDis表插入数据异常", $"Data={JsonConvert.SerializeObject(task)}", ex);
//                SaveErrToText("添加WMSTaskDis失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int InsertAddPod(WCSAddPodModel adp)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                context.AddPod.Add(adp);
//                int result = context.SaveChanges();
//                LogInfo("插入AddPod", $"Data={JsonConvert.SerializeObject(adp)}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("向AddPod表插入数据异常", $"Data={JsonConvert.SerializeObject(adp)}", ex);
//                SaveErrToText("添加AddPod失败", ex.Message);
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 通用插入方法（使用原始 SQL，支持动态表名）
//        /// </summary>
//        public static int ExecuteInsert<T>(string TableName, T Data)
//        {
//            try
//            {
//                var properties = typeof(T).GetProperties();
//                var fields = string.Join(",", properties.Select(p => p.Name));
//                var values = string.Join(",", properties.Select(p => $"@{p.Name}"));

//                var sql = $"INSERT INTO {TableName} ({fields}) VALUES ({values})";

//                using var context = CreateDbContext();
//                // 使用 ExecuteSqlRaw 执行参数化插入
//                var parameters = properties.ToDictionary(p => p.Name, p => p.GetValue(Data) ?? DBNull.Value);
//                int result = context.Database.ExecuteSqlRaw(sql, parameters);

//                LogInfo("通用插入", $"Table={TableName}, Data={JsonConvert.SerializeObject(Data)}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError($"向{TableName}表插入数据异常", $"Data={JsonConvert.SerializeObject(Data)}", ex);
//                SaveErrToText("插入数据时发生错误", ex.Message);
//                return 0;
//            }
//        }
//        #endregion

//        #region 更新方法
//        public static int UpdateStatusAsync(TescorrespondsWms tcm)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.TescorrespondsWms.FirstOrDefault(t => t.WcsId == tcm.WcsId);
//                if (existing == null) return 0;

//                existing.TesTaskID = tcm.TesTaskID;
//                existing.TargetLocation = tcm.TargetLocation;
//                existing.Statu = tcm.Statu;
//                existing.CompletionTime = tcm.CompletionTime;

//                int result = context.SaveChanges();
//                LogInfo("更新TescorrespondsWms状态", $"WcsId={tcm.WcsId}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("更新TescorrespondsWms表任务状态异常", $"Data={JsonConvert.SerializeObject(tcm)}", ex);
//                SaveErrToText("修改状态时发生错误", ex.Message);
//                return 0;
//            }
//        }

//        public static int UpdateStatusTesIDAsync(TescorrespondsWms tcm)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.TescorrespondsWms.FirstOrDefault(t => t.WmsTaskID == tcm.WmsTaskID);
//                if (existing == null) return 0;

//                existing.Statu = tcm.Statu;
//                existing.TesTaskID = tcm.TesTaskID;
//                existing.CompletionTime = tcm.CompletionTime;

//                int result = context.SaveChanges();
//                LogInfo("更新TescorrespondsWms状态和TesTaskID", $"WmsTaskID={tcm.WmsTaskID}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("更新TescorrespondsWms表任务状态异常", $"Data={JsonConvert.SerializeObject(tcm)}", ex);
//                SaveErrToText("修改状态时发生错误", ex.Message);
//                return 0;
//            }
//        }

//        public static int UpdateTargetLocationAndTesTaskIdAsync(TescorrespondsWms tcm)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.TescorrespondsWms.FirstOrDefault(t => t.WmsTaskID == tcm.WmsTaskID);
//                if (existing == null) return 0;

//                existing.TesTaskID = tcm.TesTaskID;
//                existing.TargetLocation = tcm.TargetLocation;
//                existing.CompletionTime = tcm.CompletionTime;

//                int result = context.SaveChanges();
//                LogInfo("更新目标位置和TesTaskID", $"WmsTaskID={tcm.WmsTaskID}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("更新TescorrespondsWms表任务目标位置和TesTaskID异常",
//                    $"WmsTaskID={tcm.WmsTaskID}, TesTaskID={tcm.TesTaskID}, TargetLocation={tcm.TargetLocation}", ex);
//                SaveErrToText("更新任务目标位置和TesTaskID时发生错误", ex.Message);
//                return 0;
//            }
//        }

//        public static int UpdateAgvOccupyStatusAsync(AgvPalletStation aps)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.AgvPalletStation.FirstOrDefault(a => a.PSStationCode == aps.PSStationCode);
//                if (existing == null) return 0;

//                existing.OccupyStatus = aps.OccupyStatus;
//                return context.SaveChanges();
//            }
//            catch (Exception ex)
//            {
//                LogError("更新AgvPalletStation表占用状态异常", $"Data={JsonConvert.SerializeObject(aps)}", ex);
//                SaveErrToText("修改占用状态失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int UpdateAgvPalletStationOccAsync(AgvPalletStation aps)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.AgvPalletStation.FirstOrDefault(a => a.id == aps.id);
//                if (existing == null) return 0;

//                existing.OccupyStatus = aps.OccupyStatus;
//                existing.Podid = aps.Podid;
//                return context.SaveChanges();
//            }
//            catch (Exception ex)
//            {
//                LogError("更新AgvPalletStation表容器号异常", $"Data={JsonConvert.SerializeObject(aps)}", ex);
//                SaveErrToText("修改容器号失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int UpdateStationstatusAsync(Stationstatus aps)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.Stationstatus.FirstOrDefault(s => s.StationCode == aps.StationCode);
//                if (existing == null) return 0;

//                existing.Status = aps.Status;
//                return context.SaveChanges();
//            }
//            catch (Exception ex)
//            {
//                LogError("更新Stationstatus表流向状态异常", $"Data={JsonConvert.SerializeObject(aps)}", ex);
//                SaveErrToText("修改流向状态失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int UpdateConvevisuStatusAsync(ConveVisu visu)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var existing = context.ConveVisu.FirstOrDefault(c => c.DataGuid == visu.DataGuid);
//                if (existing == null) return 0;

//                existing.Status = visu.Status;
//                existing.DeviceNum = visu.DeviceNum;
//                existing.Podid = visu.Podid;
//                return context.SaveChanges();
//            }
//            catch (Exception ex)
//            {
//                LogError("更新ConveVisu表画面显示状态异常", $"Data={JsonConvert.SerializeObject(visu)}", ex);
//                SaveErrToText("修改画面状态失败", ex.Message);
//                return 0;
//            }
//        }

//        /// <summary>
//        /// 通用更新方法（使用原始 SQL，支持动态表名）
//        /// </summary>
//        public static int ExecuteUpdate<T>(string TableName, T AgoData, T NewData)
//        {
//            try
//            {
//                var agoProps = typeof(T).GetProperties();
//                var newProps = typeof(T).GetProperties();

//                var setClause = string.Join(", ", newProps.Select(p => $"{p.Name} = @New_{p.Name}"));
//                var whereClause = string.Join(" AND ", agoProps.Select(p => $"{p.Name} = @Old_{p.Name}"));

//                var sql = $"UPDATE {TableName} SET {setClause} WHERE {whereClause}";

//                var parameters = new Dictionary<string, object>();
//                foreach (var p in agoProps)
//                    parameters[$"Old_{p.Name}"] = p.GetValue(AgoData) ?? DBNull.Value;
//                foreach (var p in newProps)
//                    parameters[$"New_{p.Name}"] = p.GetValue(NewData) ?? DBNull.Value;

//                using var context = CreateDbContext();
//                int result = context.Database.ExecuteSqlRaw(sql, parameters);

//                LogInfo("通用更新", $"Table={TableName}, Ago={JsonConvert.SerializeObject(AgoData)}, New={JsonConvert.SerializeObject(NewData)}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError($"更新{TableName}表数据异常", $"Ago={JsonConvert.SerializeObject(AgoData)}, New={JsonConvert.SerializeObject(NewData)}", ex);
//                return 0;
//            }
//        }
//        #endregion

//        #region 删除方法
//        public static int ExecuteDelete(string TableName, Dictionary<string, string> WhereParameter)
//        {
//            try
//            {
//                var conditions = string.Join(" AND ", WhereParameter.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));
//                var sql = $"DELETE FROM {TableName} WHERE {conditions}";
//                var parameters = WhereParameter.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

//                using var context = CreateDbContext();
//                int result = context.Database.ExecuteSqlRaw(sql, parameters);

//                LogInfo("通用删除", $"Table={TableName}, Where={JsonConvert.SerializeObject(WhereParameter)}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError($"从{TableName}表删除数据异常", $"Where={JsonConvert.SerializeObject(WhereParameter)}", ex);
//                SaveErrToText("删除失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int DeleteDataAsync(int id)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var entity = context.TescorrespondsWms.FirstOrDefault(t => t.id == id);
//                if (entity == null) return 0;
//                context.TescorrespondsWms.Remove(entity);
//                int result = context.SaveChanges();
//                LogInfo("删除TescorrespondsWms", $"id={id}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("从TescorrespondsWms表删除数据异常", $"id={id}", ex);
//                SaveErrToText("删除失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int DeleteWMSTaskDisAsync(string WmsId)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var entities = context.WMSTaskDis.Where(w => w.WmsId == WmsId).ToList();
//                if (!entities.Any()) return 0;
//                context.WMSTaskDis.RemoveRange(entities);
//                int result = context.SaveChanges();
//                LogInfo("删除WMSTaskDis", $"WmsId={WmsId}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("从WMSTaskDis表删除数据异常", $"WmsId={WmsId}", ex);
//                SaveErrToText("删除WMSTaskDis失败", ex.Message);
//                return 0;
//            }
//        }

//        public static int DeleteAddPodModel(string podid)
//        {
//            try
//            {
//                using var context = CreateDbContext();
//                var entities = context.AddPod.Where(a => a.PodID == podid).ToList();
//                if (!entities.Any()) return 0;
//                context.AddPod.RemoveRange(entities);
//                int result = context.SaveChanges();
//                LogInfo("删除AddPod", $"podid={podid}, Result={result}");
//                return result;
//            }
//            catch (Exception ex)
//            {
//                LogError("从AddPod表删除数据异常", $"podid={podid}", ex);
//                SaveErrToText("删除AddPod失败", ex.Message);
//                return 0;
//            }
//        }
//        #endregion

//        #region 分页查询
//        public static async Task<QueryModel<TescorrespondsWms>> GetPagedTaskList(TaskQueryDto taskQueryDto)
//        {
//            using var context = CreateDbContext();

//            // 构建 WHERE 子句的参数化条件
//            var sqlBuilder = new StringBuilder("SELECT * FROM tescorresponds_wms WHERE 1=1");
//            var parameters = new List<NpgsqlParameter>();

//            // 辅助方法添加条件
//            void AddCondition(string condition, object value)
//            {
//                var paramName = $"@p{parameters.Count}";
//                sqlBuilder.Append($" AND {condition}");
//                parameters.Add(new NpgsqlParameter(paramName, value));
//            }

//            // 基础日期范围
//            if (!string.IsNullOrEmpty(taskQueryDto.startDate) || !string.IsNullOrEmpty(taskQueryDto.endDate))
//            {
//                if (DateTime.TryParse(taskQueryDto.startDate, out var start))
//                    AddCondition("creation_time >= @start", start);
//                if (DateTime.TryParse(taskQueryDto.endDate, out var end))
//                    AddCondition("creation_time <= @end", end.Date.AddDays(1).AddTicks(-1));
//            }
//            else
//            {
//                var today = DateTime.Today;
//                AddCondition("creation_time >= @today", today);
//                AddCondition("creation_time < @tomorrow", today.AddDays(1));
//            }

//            // 其他过滤条件（状态、类型、关键词等）... 类似添加

//            // 执行时长条件（使用 PostgreSQL 的 EXTRACT）
//            if (taskQueryDto.minDuration.HasValue)
//            {
//                var min = taskQueryDto.minDuration.Value;
//                sqlBuilder.Append(" AND EXTRACT(EPOCH FROM (COALESCE(completion_time, NOW()) - creation_time)) / 60 >= @min");
//                parameters.Add(new NpgsqlParameter("@min", min));
//            }
//            if (taskQueryDto.maxDuration.HasValue)
//            {
//                var max = taskQueryDto.maxDuration.Value;
//                sqlBuilder.Append(" AND EXTRACT(EPOCH FROM (COALESCE(completion_time, NOW()) - creation_time)) / 60 <= @max");
//                parameters.Add(new NpgsqlParameter("@max", max));
//            }

//            // 异常类型
//            if (!string.IsNullOrEmpty(taskQueryDto.exceptionType))
//            {
//                if (taskQueryDto.exceptionType == "超时任务")
//                {
//                    var completedStatuses = new[] { "到达目的地", "拆膜完成", "异常处理完成" };
//                    sqlBuilder.Append(" AND statu NOT IN (");
//                    for (int i = 0; i < completedStatuses.Length; i++)
//                    {
//                        if (i > 0) sqlBuilder.Append(",");
//                        var paramName = $"@status{i}";
//                        sqlBuilder.Append(paramName);
//                        parameters.Add(new NpgsqlParameter(paramName, completedStatuses[i]));
//                    }
//                    sqlBuilder.Append(") AND creation_time < @cutoff");
//                    parameters.Add(new NpgsqlParameter("@cutoff", DateTime.UtcNow.AddMinutes(-30)));
//                }
//                else
//                {
//                    AddCondition("statu = @exceptionType", taskQueryDto.exceptionType);
//                }
//            }

//            // 排序
//            string orderBy = taskQueryDto.sortBy switch
//            {
//                "CreationTime" => "creation_time",
//                "CompletionTime" => "completion_time",
//                "WmsTaskID" => "wms_task_id",
//                "TesTaskID" => "tes_task_id",
//                "Duration" => "EXTRACT(EPOCH FROM (COALESCE(completion_time, NOW()) - creation_time)) / 60",
//                _ => "creation_time DESC"
//            };
//            string sortOrder = string.Equals(taskQueryDto.sortOrder, "ascending", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
//            sqlBuilder.Append($" ORDER BY {orderBy} {sortOrder}");

//            // 查询总数（需要单独的 COUNT 查询）
//            string countSql = sqlBuilder.ToString().Replace("SELECT *", "SELECT COUNT(1)");
//            var totalCount = await context.Database.ExecuteSqlRawAsync(countSql, parameters.ToArray());

//            // 分页查询
//            sqlBuilder.Append(" OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY");
//            parameters.Add(new NpgsqlParameter("@offset", (taskQueryDto.page - 1) * taskQueryDto.pageSize));
//            parameters.Add(new NpgsqlParameter("@pagesize", taskQueryDto.pageSize));

//            var data = await context.TescorrespondsWms
//                .FromSqlRaw(sqlBuilder.ToString(), parameters.ToArray())
//                .ToListAsync();

//            return new QueryModel<TescorrespondsWms>
//            {
//                list = data,
//                total = totalCount,
//                page = taskQueryDto.page,
//                pageSize = taskQueryDto.pageSize
//            };
//        }

//        public static async Task<QueryModel<TescorrespondsWms>> GetHistoryTaskList(TaskHistoryQueryParams queryParams)
//        {
//            using var context = CreateDbContext();
//            var query = context.TescorrespondsWms.AsQueryable();

//            // ---------- 日期范围 ----------
//            if (!string.IsNullOrEmpty(queryParams.startDate) || !string.IsNullOrEmpty(queryParams.endDate))
//            {
//                DateTime? start = null, end = null;
//                if (!string.IsNullOrEmpty(queryParams.startDate) && DateTime.TryParse(queryParams.startDate, out var s))
//                    start = s;
//                if (!string.IsNullOrEmpty(queryParams.endDate) && DateTime.TryParse(queryParams.endDate, out var e))
//                    end = e.Date.AddDays(1).AddTicks(-1);

//                if (start.HasValue && end.HasValue)
//                    query = query.Where(t => t.CreationTime >= start && t.CreationTime <= end);
//                else if (start.HasValue)
//                    query = query.Where(t => t.CreationTime >= start);
//                else if (end.HasValue)
//                    query = query.Where(t => t.CreationTime <= end);
//            }
//            else
//            {
//                var endDate = DateTime.Today;
//                var startDate = endDate.AddDays(-7);
//                query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
//            }

//            // 其他过滤条件（与 GetPagedTaskList 类似，略去重复）
//            // 实际使用时请按需添加状态、类型、关键词等过滤

//            // ---------- 排序 ----------
//            bool ascending = string.Equals(queryParams.sortOrder, "ascending", StringComparison.OrdinalIgnoreCase);
//            query = queryParams.sortBy switch
//            {
//                "CreationTime" => ascending ? query.OrderBy(t => t.CreationTime) : query.OrderByDescending(t => t.CreationTime),
//                "CompletionTime" => ascending ? query.OrderBy(t => t.CompletionTime) : query.OrderByDescending(t => t.CompletionTime),
//                "WmsTaskID" => ascending ? query.OrderBy(t => t.WmsTaskID) : query.OrderByDescending(t => t.WmsTaskID),
//                "TesTaskID" => ascending ? query.OrderBy(t => t.TesTaskID) : query.OrderByDescending(t => t.TesTaskID),
//                _ => query.OrderByDescending(t => t.CreationTime)
//            };

//            int totalCount = await query.CountAsync();
//            var data = await query
//                .Skip((queryParams.page - 1) * queryParams.pageSize)
//                .Take(queryParams.pageSize)
//                .ToListAsync();

//            return new QueryModel<TescorrespondsWms>
//            {
//                list = data,
//                total = totalCount,
//                page = queryParams.page,
//                pageSize = queryParams.pageSize
//            };
//        }
//        #endregion
//    }
//}