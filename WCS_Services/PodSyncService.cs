using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WCS_Models.TESModel;
using WCS_Models.WCSModel;

namespace WCS_Services
{
    public static class PodSyncService
    {
        /// <summary>
        /// 同步所有点位的容器号（先清空，再根据TES返回重新填充）
        /// </summary>
        public static async Task SyncPodIDsAsync()
        {
            WriteLog("========== 开始同步容器号 ==========");

            // 1. 清空所有旧容器号（数据库和内存缓存）
            StoragelocationCollection.ClearAllPodIDs();
            WriteLog("已清空所有旧容器号");

            // 2. 重新加载点位列表（清空后内存缓存已更新）
            var allLocations = StoragelocationCollection.DataList;
            if (allLocations == null || !allLocations.Any())
            {
                WriteLog("警告：没有货位数据，同步终止。");
                return;
            }
            WriteLog($"总货位数: {allLocations.Count}");

            // 3. 从 TES 获取容器
            var request = new ContainerInfoModel
            {
                warehouseID = "HETU",
                regionCode = "",
                clientCode = "WCS",
                pageSize = 10000,
                pageNum = 1
            };

            var response = await ToTesApiService.SendAgvTaskAsync(request);
            if (response.returnCode != 0)
            {
                WriteLog($"TES 接口返回错误: {response.returnMsg} (Code: {response.returnCode})");
                throw new Exception($"TES 接口返回错误: {response.returnMsg}");
            }

            var podList = response.data?.podList;
            if (podList == null || !podList.Any())
            {
                WriteLog("TES 返回的容器列表为空，同步结束。");
                return;
            }

            WriteLog($"TES 返回容器总数: {response.data.count}");
            WriteLog($"实际收到容器记录数: {podList.Count}");

            // 4. 遍历容器，匹配并更新点位
            int matchSuccessCount = 0;
            int nullCurNodeCodeCount = 0;
            int noMatchCount = 0;

            // 为提高匹配效率，先构建一个 Location 字典（key: Location, value: Storagelocation）
            var locationDict = allLocations
                .Where(l => !string.IsNullOrEmpty(l.Location))
                .ToDictionary(l => l.Location.Trim(), l => l, StringComparer.OrdinalIgnoreCase);

            foreach (var pod in podList)
            {
                // 跳过 curNodeCode 为空的容器
                if (string.IsNullOrEmpty(pod.curNodeCode))
                {
                    nullCurNodeCodeCount++;
                    continue;
                }

                string nodeKey = pod.curNodeCode.Trim();
                if (locationDict.TryGetValue(nodeKey, out var location))
                {
                    // 匹配成功，更新 PodID
                    location.PodID = pod.podID;
                    matchSuccessCount++;
                    // 持久化到数据库并刷新缓存
                    StoragelocationCollection.Update(location);
                }
                else
                {
                    noMatchCount++;
                    // 不记录详细失败日志，仅统计总数
                }
            }

            // 5. 统计并输出结果
            WriteLog($"同步完成。");
            WriteLog($"  - 成功匹配并更新容器数: {matchSuccessCount}");
            WriteLog($"  - curNodeCode 为空的容器数: {nullCurNodeCodeCount}");
            WriteLog($"  - 有 curNodeCode 但未匹配到货位的容器数: {noMatchCount}");
            WriteLog($"  - 货位中存在但未匹配到的数量: {allLocations.Count - matchSuccessCount}（含空Location）");

            WriteLog("========== 同步结束 ==========");
        }

        private static void WriteLog(string message)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, $"PodSync_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { /* 忽略日志写入错误 */ }
        }
    }
}