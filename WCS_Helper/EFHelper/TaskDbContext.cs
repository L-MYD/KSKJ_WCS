using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WCS_Models.LoginViewModel;
using WCS_Models.PLCModel;
using WCS_Models.SqlModel;
using WCS_Models.WCSModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Models.WMSModel;


namespace WCS_Helper.Mapper
{
    public static class TaskDbContext
    {
        private static readonly string ConnectionString = ConfigurationHelper.GetConnectionString();

        #region 辅助方法：创建 DbContext 实例
        private static WcsDbContext CreateDbContext()
        {
            // 使用 OnConfiguring 中的配置，无需传入连接字符串
            return new WcsDbContext();
        }
        #endregion

        #region 查询方法
        public static TescorrespondsWms QueryDataAsync(TescorrespondsWms tcw)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.TescorrespondsWms
                    .FirstOrDefault(t => t.id == tcw.id);
                return result;
            }
            catch (Exception ex)
            {
                LogError("查询TescorrespondsWms表失败", $"id={tcw.id}", ex);
                Logger.SaveErrToText("查询数据时发生错误", ex.Message);
                return new TescorrespondsWms();
            }
        }

        public static List<T> SelectContent<T>(string TableName) where T : class
        {
            try
            {
                using var context = CreateDbContext();
                // 通过 DbContext 动态获取 DbSet
                var dbSet = context.Set<T>();
                return dbSet.ToList();
            }
            catch (Exception ex)
            {
                LogError($"查询{TableName}表失败", $"TableName={TableName}", ex);
                Logger.SaveErrToText($"查询{TableName}表失败", ex.Message);
                return new List<T>();
            }
        }

        public static TescorrespondsWms GetByTesTaskIdAsync(string tesTaskId)
        {
            try
            {
                using var context = CreateDbContext();
                return context.TescorrespondsWms
                    .FirstOrDefault(t => t.TesTaskID == tesTaskId);
            }
            catch (Exception ex)
            {
                LogError("根据TesTaskID查询数据异常", $"TesTaskID={tesTaskId}", ex);
                Logger.SaveErrToText("根据TesTaskID查询失败", ex.Message);
                return null;
            }
        }

        public static WCSAddPodModel GetAddPodModel(string podid)
        {
            try
            {
                using var context = CreateDbContext();
                return context.AddPod.FirstOrDefault(a => a.PodID == podid);
            }
            catch (Exception ex)
            {
                LogError("根据Podid查询数据异常", $"Podid={podid}", ex);
                Logger.SaveErrToText("根据Podid查询失败", ex.Message);
                return null;
            }
        }

        public static TescorrespondsWms GetByTescontainerNoAsync(string containerNo)
        {
            try
            {
                using var context = CreateDbContext();
                return context.TescorrespondsWms
                    .Where(t => t.Podid == containerNo)
                    .OrderByDescending(t => t.id)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogError("根据ContainerNo查询数据异常", $"ContainerNo={containerNo}", ex);
                Logger.SaveErrToText("根据ContainerNo查询失败", ex.Message);
                return null;
            }
        }

        public static List<TescorrespondsWms> GetAllPendingTasksFromDatabase(string TargetLocation)
        {
            try
            {
                using var context = CreateDbContext();
                return context.TescorrespondsWms
                    .Where(t => t.Statu != "输送机出库完成"
                                && t.WmsTaskType == "出库任务"
                                && t.TargetLocation == TargetLocation)
                    .ToList();
            }
            catch (Exception ex)
            {
                LogError("获取待执行任务失败", $"TargetLocation={TargetLocation}", ex);
                Logger.SaveErrToText("查询待执行任务失败", ex.Message);
                return new List<TescorrespondsWms>();
            }
        }

        public static TescorrespondsWms GetByTargetLocationAsync(string TargetLocation, string Podid)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.TescorrespondsWms
                    .Where(t => t.TargetLocation == TargetLocation
                                && t.Podid == Podid
                                && t.WmsTaskType == "出库任务")
                    .OrderByDescending(t => t.id)
                    .FirstOrDefault();

                LogInfo("根据CurrentPosition查询结果",
                    $"TargetLocation={TargetLocation}, Podid={Podid}, Result={JsonConvert.SerializeObject(result)}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("根据CurrentPosition查询数据异常",
                    $"TargetLocation={TargetLocation}, Podid={Podid}", ex);
                Logger.SaveErrToText("根据CurrentPosition查询失败", ex.Message);
                return null;
            }
        }

        public static TescorrespondsWms GetByWmsTaskIdAsync(string wmsTaskId)
        {
            try
            {
                using var context = CreateDbContext();
                return context.TescorrespondsWms
                    .FirstOrDefault(t => t.WmsTaskID == wmsTaskId);
            }
            catch (Exception ex)
            {
                LogError("根据WmsTaskID查询数据异常", $"WmsTaskID={wmsTaskId}", ex);
                Logger.SaveErrToText("根据WmsTaskID查询失败", ex.Message);
                return null;
            }
        }

        public static Aisles GetByWmsReferenceAsync(string WmsReference)
        {
            try
            {
                using var context = CreateDbContext();
                return context.Aisles
                    .FirstOrDefault(a => a.WmsReference == WmsReference);
            }
            catch (Exception ex)
            {
                LogError("根据WmsReference查询数据异常", $"WmsReference={WmsReference}", ex);
                Logger.SaveErrToText("根据WmsReference查询失败", ex.Message);
                return null;
            }
        }

        public static StationCodeWithNum GetStationCodeWithNumByCode(string code)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.StationCodeWithNum
                    .FirstOrDefault(s => s.code == code);
                LogInfo("根据站点名称查询站点编号",
                    $"code={code}, Result={JsonConvert.SerializeObject(result)}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("根据站点名称查询站点编号异常", $"code={code}", ex);
                Logger.SaveErrToText("根据站点名称查询站点编号失败", ex.Message);
                return null;
            }
        }

        public static TescorrespondsWms GetTaskNumWithTaskType(string TargetLocation)
        {
            // 注意原 SQL 中 Statu 条件使用了双引号，可能是错误，这里按字符串处理
            try
            {
                using var context = CreateDbContext();
                return context.TescorrespondsWms
                    .Where(t => t.TargetLocation == TargetLocation
                                && t.Statu != "到达目的地"
                                && t.Statu != "任务失败"
                                && t.Statu != "托盘空取"
                                && t.Statu != "任务已取消")
                    .OrderByDescending(t => t.CreationTime)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogError("根据任务类型查询任务数量", $"TargetLocation={TargetLocation}", ex);
                Logger.SaveErrToText("根据任务类型查询任务数量失败", ex.Message);
                return null;
            }
        }

        public static StationCodeWithNum GetStationCodeWithNumByNum(string nodes_uni)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.StationCodeWithNum
                    .FirstOrDefault(s => s.nodes_uni == nodes_uni);
                LogInfo("根据站点编号查询站点名称",
                    $"nodes_uni={nodes_uni}, Result={JsonConvert.SerializeObject(result)}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("根据站点编号查询站点名称异常", $"nodes_uni={nodes_uni}", ex);
                Logger.SaveErrToText("根据站点编号查询站点名称失败", ex.Message);
                return null;
            }
        }

        public static EmptyPodIDApply GetEmptyPodIDApplyByPodID(string podid)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.EmptyPodIDApply
                    .FirstOrDefault(s => s.PodID == podid);
                //LogInfo("根据站点编号查询站点名称",
                //    $"PodID={podid}, Result={JsonConvert.SerializeObject(result)}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("根据站点编号查询站点名称异常", $"nodes_uni={podid}", ex);
                Logger.SaveErrToText("根据站点编号查询站点名称失败", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 根据目的地和任务类型查询任务
        /// </summary>
        /// <param name="stationCode"></param>
        /// <param name="tasktype"></param>
        /// <returns></returns>
        public static TescorrespondsWms GetByTesstationCodeTaskTypeAsync(string stationCode, string tasktype, string statu)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.TescorrespondsWms
                    .FirstOrDefault(t => t.TargetLocation == stationCode
                                         && t.WmsTaskType == tasktype
                                         && t.Statu == statu);
                LogInfo("根据站点、状态和容器号查询",
                    $"TargetLocation={stationCode}, WmsTaskType={tasktype}, Statu == {statu}, Result={JsonConvert.SerializeObject(result)}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("根据站点、状态和容器号查询异常",
                    $"TargetLocation={stationCode}, WmsTaskType={tasktype}, Statu == {statu}", ex);
                Logger.SaveErrToText("根据站点、状态和容器号查询失败", ex.Message);
                return null;
            }
        }

        public static TescorrespondsWms GetByTesstationCodeAsync(string stationCode, string statu, string podid)
        {
            try
            {
                using var context = CreateDbContext();
                var result = context.TescorrespondsWms
                    .FirstOrDefault(t => t.CurrentPosition == stationCode
                                         && t.Statu == statu
                                         && t.Podid == podid);
                LogInfo("根据站点、状态和容器号查询",
                    $"stationCode={stationCode}, statu={statu}, podid={podid}, Result={JsonConvert.SerializeObject(result)}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("根据站点、状态和容器号查询异常",
                    $"stationCode={stationCode}, statu={statu}, podid={podid}", ex);
                Logger.SaveErrToText("根据站点、状态和容器号查询失败", ex.Message);
                return null;
            }
        }
        #endregion

        #region 插入方法
        public static int InsertDataAsync(TescorrespondsWms tcm)
        {
            try
            {
                using var context = CreateDbContext();
                context.TescorrespondsWms.Add(tcm);
                var result = context.SaveChanges();
                LogInfo("插入TescorrespondsWms", $"Data={JsonConvert.SerializeObject(tcm)}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("向TescorrespondsWms表插入数据异常",
                    $"Data={JsonConvert.SerializeObject(tcm)}", ex);
                Logger.SaveErrToText("添加数据时发生错误", ex.Message);
                return 0;
            }
        }

        public static int InsertAddEmptyPodIDApply(EmptyPodIDApply tcm)
        {
            try
            {
                using var context = CreateDbContext();
                context.EmptyPodIDApply.Add(tcm);
                var result = context.SaveChanges();
                LogInfo("EmptyPodIDApply", $"Data={JsonConvert.SerializeObject(tcm)}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("向EmptyPodIDApply表插入数据异常",
                    $"Data={JsonConvert.SerializeObject(tcm)}", ex);
                Logger.SaveErrToText("添加数据时发生错误", ex.Message);
                return 0;
            }
        }

        //public static int InsertWMSTaskDis(WMSTaskDis task)
        //{
        //    try
        //    {
        //        using var context = CreateDbContext();
        //        context.WMSTaskDis.Add(task);
        //        var result = context.SaveChanges();
        //        LogInfo("插入WMSTaskDis", $"Data={JsonConvert.SerializeObject(task)}, Result={result}");
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogError("向WMSTaskDis表插入数据异常",
        //            $"Data={JsonConvert.SerializeObject(task)}", ex);
        //        Logger.SaveErrToText("添加WMSTaskDis失败", ex.Message);
        //        return 0;
        //    }
        //}

        public static int InsertAddPod(WCSAddPodModel adp)
        {
            try
            {
                using var context = CreateDbContext();
                context.AddPod.Add(adp);
                var result = context.SaveChanges();
                LogInfo("插入AddPod", $"Data={JsonConvert.SerializeObject(adp)}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("向AddPod表插入数据异常",
                    $"Data={JsonConvert.SerializeObject(adp)}", ex);
                Logger.SaveErrToText("添加AddPod失败", ex.Message);
                return 0;
            }
        }
        #endregion

        #region 更新方法
        public static int UpdateStatusAsync(TescorrespondsWms tcm)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.TescorrespondsWms.FirstOrDefault(t => t.WcsId == tcm.WcsId);
                if (existing == null) return 0;

                // 更新字段
                existing.TesTaskID = tcm.TesTaskID;
                existing.TargetLocation = tcm.TargetLocation;
                existing.Statu = tcm.Statu;
                existing.CompletionTime = tcm.CompletionTime;

                var result = context.SaveChanges();
                LogInfo("更新TescorrespondsWms状态", $"WcsId={tcm.WcsId}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新TescorrespondsWms表任务状态异常",
                    $"Data={JsonConvert.SerializeObject(tcm)}", ex);
                Logger.SaveErrToText("修改状态时发生错误", ex.Message);
                return 0;
            }
        }

        public static int UpdateStatusTesIDAsync(TescorrespondsWms tcm)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.TescorrespondsWms.FirstOrDefault(t => t.WmsTaskID == tcm.WmsTaskID);
                if (existing == null) return 0;

                existing.Statu = tcm.Statu;
                existing.TesTaskID = tcm.TesTaskID;
                existing.CompletionTime = tcm.CompletionTime;

                var result = context.SaveChanges();
                LogInfo("更新TescorrespondsWms状态和TesTaskID", $"WmsTaskID={tcm.WmsTaskID}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新TescorrespondsWms表任务状态异常",
                    $"Data={JsonConvert.SerializeObject(tcm)}", ex);
                Logger.SaveErrToText("修改状态时发生错误", ex.Message);
                return 0;
            }
        }

        public static int UpdateTargetLocationAndTesTaskIdAsync(TescorrespondsWms tcm)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.TescorrespondsWms.FirstOrDefault(t => t.WmsTaskID == tcm.WmsTaskID);
                if (existing == null) return 0;

                existing.TesTaskID = tcm.TesTaskID;
                existing.TargetLocation = tcm.TargetLocation;
                existing.CompletionTime = tcm.CompletionTime;

                var result = context.SaveChanges();
                LogInfo("更新目标位置和TesTaskID", $"WmsTaskID={tcm.WmsTaskID}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新TescorrespondsWms表任务目标位置和TesTaskID异常",
                    $"WmsTaskID={tcm.WmsTaskID}, TesTaskID={tcm.TesTaskID}, TargetLocation={tcm.TargetLocation}", ex);
                Logger.SaveErrToText("更新任务目标位置和TesTaskID时发生错误", ex.Message);
                return 0;
            }
        }

        public static int UpdateAgvOccupyStatusAsync(AgvPalletStation aps)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.AgvPalletStation.FirstOrDefault(a => a.PSStationCode == aps.PSStationCode);
                if (existing == null) return 0;

                existing.OccupyStatus = aps.OccupyStatus;
                var result = context.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新AgvPalletStation表占用状态异常",
                    $"Data={JsonConvert.SerializeObject(aps)}", ex);
                Logger.SaveErrToText("修改占用状态失败", ex.Message);
                return 0;
            }
        }

        public static int UpdateAgvPalletStationOccAsync(AgvPalletStation aps)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.AgvPalletStation.FirstOrDefault(a => a.Id == aps.Id);
                if (existing == null) return 0;

                existing.OccupyStatus = aps.OccupyStatus;
                existing.Podid = aps.Podid;
                var result = context.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新AgvPalletStation表容器号异常",
                    $"Data={JsonConvert.SerializeObject(aps)}", ex);
                Logger.SaveErrToText("修改容器号失败", ex.Message);
                return 0;
            }
        }

        public static int UpdateStationstatusAsync(Stationstatus aps)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.Stationstatus.FirstOrDefault(s => s.StationCode == aps.StationCode);
                if (existing == null) return 0;

                existing.Status = aps.Status;
                var result = context.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新Stationstatus表流向状态异常",
                    $"Data={JsonConvert.SerializeObject(aps)}", ex);
                Logger.SaveErrToText("修改流向状态失败", ex.Message);
                return 0;
            }
        }

        public static int UpdateConvevisuStatusAsync(ConveVisu visu)
        {
            try
            {
                using var context = CreateDbContext();
                var existing = context.ConveVisu.FirstOrDefault(c => c.DataGuid == visu.DataGuid);
                if (existing == null) return 0;

                existing.Status = visu.Status;
                existing.DeviceNum = visu.DeviceNum;
                existing.Podid = visu.Podid;
                var result = context.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                LogError("更新ConveVisu表画面显示状态异常",
                    $"Data={JsonConvert.SerializeObject(visu)}", ex);
                Logger.SaveErrToText("修改画面状态失败", ex.Message);
                return 0;
            }
        }
        #endregion

        #region 删除方法
        public static int ExecuteDelete(string TableName, Dictionary<string, string> WhereParameter)
        {
            try
            {
                using var context = CreateDbContext();
                // 由于表名动态，EF Core 原生不支持动态表名，这里改用原始 SQL 执行删除
                // 注意：需要防止 SQL 注入，参数化处理
                var conditions = string.Join(" AND ", WhereParameter.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));
                var sql = $"DELETE FROM {TableName} WHERE {conditions}";
                var parameters = WhereParameter.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

                var result = context.Database.ExecuteSqlRaw(sql, parameters);
                LogInfo("执行删除", $"Table={TableName}, Conditions={JsonConvert.SerializeObject(WhereParameter)}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"从{TableName}表删除数据异常",
                    $"Where={JsonConvert.SerializeObject(WhereParameter)}", ex);
                Logger.SaveErrToText("删除失败", ex.Message);
                return 0;
            }
        }

        public static int DeleteDataAsync(int id)
        {
            try
            {
                using var context = CreateDbContext();
                var entity = context.TescorrespondsWms.FirstOrDefault(t => t.id == id);
                if (entity == null) return 0;
                context.TescorrespondsWms.Remove(entity);
                var result = context.SaveChanges();
                LogInfo("删除TescorrespondsWms", $"id={id}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("从TescorrespondsWms表删除数据异常", $"id={id}", ex);
                Logger.SaveErrToText("删除失败", ex.Message);
                return 0;
            }
        }

        public static int DeleteEmptyPodIDApply(string podid)
        {
            try
            {
                using var context = CreateDbContext();
                var entity = context.EmptyPodIDApply.FirstOrDefault(t => t.PodID == podid);
                if (entity == null) return 0;
                context.EmptyPodIDApply.Remove(entity);
                var result = context.SaveChanges();
                LogInfo("删除EmptyPodIDApply", $"id={podid}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("从EmptyPodIDApply表删除数据异常", $"id={podid}", ex);
                Logger.SaveErrToText("删除失败", ex.Message);
                return 0;
            }
        }


        public static int DeleteAddPodModel(string podid)
        {
            try
            {
                using var context = CreateDbContext();
                var entities = context.AddPod.Where(a => a.PodID == podid).ToList();
                if (!entities.Any()) return 0;
                context.AddPod.RemoveRange(entities);
                var result = context.SaveChanges();
                LogInfo("删除AddPod", $"podid={podid}, Result={result}");
                return result;
            }
            catch (Exception ex)
            {
                LogError("从AddPod表删除数据异常", $"podid={podid}", ex);
                Logger.SaveErrToText("删除AddPod失败", ex.Message);
                return 0;
            }
        }
        #endregion

        #region 分页查询
        public static async Task<QueryModel<TescorrespondsWms>> GetPagedTaskList(TaskQueryDto taskQueryDto)
        {
            using var context = CreateDbContext();
            var query = context.TescorrespondsWms.AsQueryable();

            // 日期范围处理
            if (!string.IsNullOrEmpty(taskQueryDto.startDate) || !string.IsNullOrEmpty(taskQueryDto.endDate))
            {
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (!string.IsNullOrEmpty(taskQueryDto.startDate) &&
                    DateTime.TryParse(taskQueryDto.startDate, out var parsedStart))
                {
                    startDate = parsedStart;
                }

                if (!string.IsNullOrEmpty(taskQueryDto.endDate) &&
                    DateTime.TryParse(taskQueryDto.endDate, out var parsedEnd))
                {
                    endDate = parsedEnd.Date.AddDays(1).AddSeconds(-1);
                }

                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
                }
                else if (startDate.HasValue)
                {
                    query = query.Where(t => t.CreationTime >= startDate);
                }
                else if (endDate.HasValue)
                {
                    query = query.Where(t => t.CreationTime <= endDate);
                }
            }
            else
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                query = query.Where(t => t.CreationTime >= today && t.CreationTime < tomorrow);
            }

            // 状态过滤
            if (!string.IsNullOrEmpty(taskQueryDto.status))
            {
                query = query.Where(t => t.Statu == taskQueryDto.status);
            }

            // 任务类型过滤
            if (!string.IsNullOrEmpty(taskQueryDto.type))
            {
                query = query.Where(t => t.WmsTaskType == taskQueryDto.type);
            }

            // 关键词搜索
            if (!string.IsNullOrEmpty(taskQueryDto.keyword))
            {
                query = query.Where(t => t.WmsTaskID.Contains(taskQueryDto.keyword)
                                         || t.TesTaskID.Contains(taskQueryDto.keyword));
            }

            // 容器号搜索
            if (!string.IsNullOrEmpty(taskQueryDto.podId))
            {
                query = query.Where(t => t.Podid.Contains(taskQueryDto.podId));
            }

            // 位置搜索
            if (!string.IsNullOrEmpty(taskQueryDto.location))
            {
                query = query.Where(t => t.CurrentPosition.Contains(taskQueryDto.location)
                                         || t.TargetLocation.Contains(taskQueryDto.location));
            }

            // 高级搜索 - 创建时间范围
            if (taskQueryDto.creationStartTime.HasValue && taskQueryDto.creationEndTime.HasValue)
            {
                var start = taskQueryDto.creationStartTime.Value;
                var end = taskQueryDto.creationEndTime.Value.Date.AddDays(1).AddTicks(-1); // 当天的最后一刻
                query = query.Where(t => t.CreationTime >= start && t.CreationTime <= end);
            }

            // 高级搜索 - 完成时间范围
            if (taskQueryDto.completionStartTime.HasValue && taskQueryDto.completionEndTime.HasValue)
            {
                var start = taskQueryDto.completionStartTime.Value;
                var end = taskQueryDto.completionEndTime.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.CompletionTime.HasValue
                                       && t.CompletionTime.Value >= start
                                       && t.CompletionTime.Value <= end);
            }

            // 执行时长范围（需在内存中计算，或使用原始SQL，这里简单处理：不实现）
            // 原逻辑使用了 DATEDIFF，EF Core 可能无法直接翻译，若需精确，可考虑原始SQL
            // 暂时忽略时长条件，或者转换为内存计算（但效率低）

            // 异常类型过滤
            if (!string.IsNullOrEmpty(taskQueryDto.exceptionType))
            {
                if (taskQueryDto.exceptionType == "超时任务")
                {
                    // 计算 30 分钟前的时间点
                    var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
                    var completedStatuses = new[] { "到达目的地", "拆膜完成", "异常处理完成" };

                    query = query.Where(t => !completedStatuses.Contains(t.Statu)
                                             && t.CreationTime < cutoffTime);
                }
                else
                {
                    query = query.Where(t => t.Statu == taskQueryDto.exceptionType);
                }
            }

            // 排序
            if (!string.IsNullOrEmpty(taskQueryDto.sortBy))
            {
                var sortOrder = string.Equals(taskQueryDto.sortOrder, "ascending", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
                switch (taskQueryDto.sortBy)
                {
                    case "CreationTime":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.CreationTime) : query.OrderByDescending(t => t.CreationTime);
                        break;
                    case "CompletionTime":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.CompletionTime) : query.OrderByDescending(t => t.CompletionTime);
                        break;
                    case "WmsTaskID":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.WmsTaskID) : query.OrderByDescending(t => t.WmsTaskID);
                        break;
                    case "TesTaskID":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.TesTaskID) : query.OrderByDescending(t => t.TesTaskID);
                        break;
                    default:
                        query = query.OrderByDescending(t => t.CreationTime);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(t => t.CreationTime);
            }

            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((taskQueryDto.page - 1) * taskQueryDto.pageSize)
                .Take(taskQueryDto.pageSize)
                .ToListAsync();

            // 计算 Duration（如果需要）
            foreach (var item in data)
            {
                // 可以添加 Duration 计算属性，但原实体中没有，此处忽略
            }

            return new QueryModel<TescorrespondsWms>
            {
                list = data,
                total = totalCount,
                page = taskQueryDto.page,
                pageSize = taskQueryDto.pageSize
            };
        }

        public static async Task<QueryModel<TescorrespondsWms>> GetHistoryTaskList(TaskHistoryQueryParams queryParams)
        {
            using var context = CreateDbContext();
            var query = context.TescorrespondsWms.AsQueryable();

            // 日期范围处理
            if (!string.IsNullOrEmpty(queryParams.startDate) || !string.IsNullOrEmpty(queryParams.endDate))
            {
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (!string.IsNullOrEmpty(queryParams.startDate) &&
                    DateTime.TryParse(queryParams.startDate, out var parsedStart))
                {
                    startDate = parsedStart;
                }

                if (!string.IsNullOrEmpty(queryParams.endDate) &&
                    DateTime.TryParse(queryParams.endDate, out var parsedEnd))
                {
                    endDate = parsedEnd.Date.AddDays(1).AddSeconds(-1);
                }

                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
                }
                else if (startDate.HasValue)
                {
                    query = query.Where(t => t.CreationTime >= startDate);
                }
                else if (endDate.HasValue)
                {
                    query = query.Where(t => t.CreationTime <= endDate);
                }
            }
            else
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-7);
                query = query.Where(t => t.CreationTime >= startDate && t.CreationTime <= endDate);
            }

            // 状态过滤
            if (!string.IsNullOrEmpty(queryParams.status))
            {
                query = query.Where(t => t.Statu == queryParams.status);
            }

            // 任务类型过滤
            if (!string.IsNullOrEmpty(queryParams.type))
            {
                query = query.Where(t => t.WmsTaskType == queryParams.type);
            }

            // 关键词搜索
            if (!string.IsNullOrEmpty(queryParams.keyword))
            {
                query = query.Where(t => t.WmsTaskID.Contains(queryParams.keyword)
                                         || t.TesTaskID.Contains(queryParams.keyword));
            }

            // 容器号搜索
            if (!string.IsNullOrEmpty(queryParams.podId))
            {
                query = query.Where(t => t.Podid.Contains(queryParams.podId));
            }

            // 位置搜索
            if (!string.IsNullOrEmpty(queryParams.location))
            {
                query = query.Where(t => t.CurrentPosition.Contains(queryParams.location)
                                         || t.TargetLocation.Contains(queryParams.location));
            }

            // 创建时间范围
            // 考虑到 TaskHistoryQueryParams 的创建/完成时间字段可能为 DateTime 类型，避免将其当作字符串处理
            if (queryParams.creationStartTime != default(DateTime) && queryParams.creationEndTime != default(DateTime))
            {
                var cs = queryParams.creationStartTime;
                var ce = queryParams.creationEndTime;
                query = query.Where(t => t.CreationTime >= cs && t.CreationTime <= ce.AddDays(1).AddSeconds(-1));
            }

            // 完成时间范围
            if (queryParams.completionStartTime != default(DateTime) && queryParams.completionEndTime != default(DateTime))
            {
                var cps = queryParams.completionStartTime;
                var cpe = queryParams.completionEndTime;
                query = query.Where(t => t.CompletionTime >= cps && t.CompletionTime <= cpe.AddDays(1).AddSeconds(-1));
            }

            // 排序
            if (!string.IsNullOrEmpty(queryParams.sortBy))
            {
                var sortOrder = string.Equals(queryParams.sortOrder, "ascending", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
                switch (queryParams.sortBy)
                {
                    case "CreationTime":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.CreationTime) : query.OrderByDescending(t => t.CreationTime);
                        break;
                    case "CompletionTime":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.CompletionTime) : query.OrderByDescending(t => t.CompletionTime);
                        break;
                    case "WmsTaskID":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.WmsTaskID) : query.OrderByDescending(t => t.WmsTaskID);
                        break;
                    case "TesTaskID":
                        query = sortOrder == "ASC" ? query.OrderBy(t => t.TesTaskID) : query.OrderByDescending(t => t.TesTaskID);
                        break;
                    default:
                        query = query.OrderByDescending(t => t.CreationTime);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(t => t.CreationTime);
            }

            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((queryParams.page - 1) * queryParams.pageSize)
                .Take(queryParams.pageSize)
                .ToListAsync();

            return new QueryModel<TescorrespondsWms>
            {
                list = data,
                total = totalCount,
                page = queryParams.page,
                pageSize = queryParams.pageSize
            };
        }
        #endregion

        #region 文件日志（保留原有方法，但可调用 Logger）
        public static void SaveErrToText(string detail, string reason)
        {
            Logger.SaveErrToText(detail, reason);
        }
        #endregion

        #region 内部日志辅助方法
        private static void LogError(string message, string details, Exception ex)
        {
            var errorLog = new LogModel
            {
                UserType = "database",
                LogType = "Database Error",
                Level = "Error",
                Message = message,
                Module = "TaskIdMapper",
                Operation = GetCallerMemberName(),
                Details = $"{details}\n异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
                UserId = "System",
                IpAddress = "System",
                CreateTime = DateTime.UtcNow,
                IsArchived = "False"
            };
            LogsDbContext.Insert(errorLog);
        }

        private static void LogInfo(string message, string details)
        {
            var infoLog = new LogModel
            {
                UserType = "database",
                LogType = "Database Info",
                Level = "Info",
                Message = message,
                Module = "TaskIdMapper",
                Operation = GetCallerMemberName(),
                Details = details,
                UserId = "System",
                IpAddress = "System",
                CreateTime = DateTime.UtcNow,
                IsArchived = "False"
            };
            LogsDbContext.Insert(infoLog);
        }

        private static string GetCallerMemberName([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            return memberName;
        }
        #endregion
    }
}