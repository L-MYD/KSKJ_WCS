using WCS_Models.PLCModel;
using WCS_Models.SqlModel;
using WCS_Models.TESModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Models.WMSModel;
using WCS_Helper;
using Newtonsoft.Json;
using PLCFunc.ConnSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCS_Models.WCSModel;
using WCS_Common;
using WCS_Models;
using System.Collections.Concurrent;
using WCS_Helper.Mapper;

using WCS_IServices;
namespace WCS_Services
{
    public class PLCService : IPLCService
    {
        private readonly ConveMessageEvent _ConveEvent = new ConveMessageEvent();

        readonly string _ConveIP;
        readonly string _ConveSymbol;

        readonly int _DB04Num;
        readonly int _DB05Num;
        readonly int _DB06Num;

        readonly int _DB04Len;
        readonly int _DB05Len;
        readonly int _DB06Len;

        readonly int _Rack;
        readonly int _Slot;

        S7Client _s7Client = new S7Client();
        private bool _isConnected = false;
        private readonly object _clientLock = new object();

        readonly ConveInfo _ConveInfo;

        private bool _RecvConnState;
        private bool _SendConnState;

        // nan_T 2026-09-09：PLCDB5Dic 会被 Recv/Send 等多个线程并发读写，
        // 原普通 Dictionary 非线程安全，并发写入可能导致死循环/数据丢失，改为 ConcurrentDictionary。
        public static ConcurrentDictionary<string, string> PLCDB5Dic = new ConcurrentDictionary<string, string>();
        static readonly string plcstationcode = "2544";
        static readonly string plcstationOutCode = "2557";
        static readonly string tesstationcodeIn = "F1-PalletLine-IN";
        static readonly string tesstationcodeOut = "F2-PalletLine-OUT";
        static readonly string wmsapiip = "http://10.10.200.90:8003/";

        public static bool PLCConnState = false;
        private readonly Dictionary<string, DateTime> _processedMessages = new Dictionary<string, DateTime>();
        private readonly TimeSpan _messageExpiry = TimeSpan.FromMinutes(5);
        // nan_T 2026-09-09：合并重复字段，原 tesservice 与 toTesApiService 是两个完全相同的实例，统一为一个
        ToTesApiService toTesApiService = new ToTesApiService();

        private static readonly ConcurrentDictionary<string, DateTime> _lastEmptyPalletApplyTime = new ConcurrentDictionary<string, DateTime>();
        private static readonly ConcurrentDictionary<string, bool> _isApplyingEmptyPallet = new ConcurrentDictionary<string, bool>();
        private static readonly ConcurrentDictionary<string, object> _stationLocks = new ConcurrentDictionary<string, object>();
        private const int EMPTY_PALLET_COOLDOWN_SECONDS = 10;

        private int _retryDelayRecv = 1000;
        private int _retryDelaySend = 1000;
        private const int MAX_RETRY_DELAY = 30000;

        public PLCService(string PLCIP)
        {
            _ConveInfo = ConveInfoColleciton.DataList.Find(x => x.PLCIP == PLCIP);

            if (_ConveInfo == null)
            {
                SaveLogText("PLCServer", "PLC服务启动失败");
                return;
            }

            _ConveIP = _ConveInfo.PLCIP;
            _Rack = _ConveInfo.Rack;
            _Slot = _ConveInfo.Slot;

            _DB04Len = _ConveInfo.DB4Len;
            _DB04Num = _ConveInfo.DB4Num;

            _DB05Len = _ConveInfo.DB5Len;
            _DB05Num = _ConveInfo.DB5Num;

            _DB06Len = _ConveInfo.DB6Len;
            _DB06Num = _ConveInfo.DB6Num;

            _ConveSymbol = _ConveInfo.PLCSymbol;

            string DB5Name = $"{_ConveIP}:DB5";
            string DB6Name = $"{_ConveIP}:DB6";

            // nan_T 2026-09-09：ConcurrentDictionary 用 TryAdd 替代 ContainsKey+Add（原写法非原子，且 Add 重复键会抛异常）
            PLCDB5Dic.TryAdd(DB5Name, "");
            PLCDB5Dic.TryAdd(DB6Name, "");

            // 设置超时时间（10秒）
            _s7Client.ConnTimeout = 10000;
            _s7Client.RecvTimeout = 10000;
            _s7Client.SendTimeout = 10000;

            SaveLogText("PLCServer", "PLC服务启动成功，超时设置为10秒");

            // nan_T 2026-09-09：Recv/Send 由 async void 改为 async Task。
            // async void 的未捕获异常会直接终止整个进程；改为 Task 后用 Task.Run 启动，
            // 并挂接 OnlyOnFaulted 延续记录致命错误，避免后台循环异常无声拖垮进程。
            Task.Run(Recv).ContinueWith(
                t => SaveErrToText("PLCServer", $"Recv 循环致命异常: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
            Task.Run(Send).ContinueWith(
                t => SaveErrToText("PLCServer", $"Send 循环致命异常: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private bool EnsureConnection()
        {
            lock (_clientLock)
            {
                if (_isConnected)
                    return true;

                try
                {
                    _s7Client.Disconnect();
                    // 等待 2 秒，让 PLC 释放连接
                    System.Threading.Thread.Sleep(2000);
                }
                catch { }

                _s7Client = new S7Client();
                _s7Client.ConnTimeout = 10000;
                _s7Client.RecvTimeout = 10000;
                _s7Client.SendTimeout = 10000;

                var result = _s7Client.ConnectTo(_ConveIP, _Rack, _Slot);
                if (result == 0)
                {
                    _isConnected = true;
                    PLCConnState = true;
                    SaveLogText("连接建立", $"成功连接到 {_ConveIP}");
                    return true;
                }
                else
                {
                    _isConnected = false;
                    PLCConnState = false;
                    string errMsg = _s7Client.ErrorText(result);
                    SaveErrToText("连接失败", $"ErrorCode:{result} ({errMsg})");
                    return false;
                }
            }
        }

        // nan_T 2026-09-09：async void → async Task，异常可被 Task 观测而不是直接崩溃进程
        async Task Recv()
        {
            while (true)
            {
                try
                {
                    if (!CheckConnection.Ping(_ConveIP))
                    {
                        lock (_clientLock)
                        {
                            _s7Client.Disconnect();
                            _isConnected = false;
                            PLCConnState = false;
                        }
                        await Task.Delay(2000);
                        continue;
                    }

                    if (!EnsureConnection())
                    {
                        await Task.Delay(_retryDelayRecv);
                        _retryDelayRecv = Math.Min(_retryDelayRecv * 2, MAX_RETRY_DELAY);
                        continue;
                    }
                    _retryDelayRecv = 1000;

                    lock (_clientLock)
                    {
                        // ---- DB5 ----
                        if (_DB05Num > 0 && _DB05Len > 0)
                        {
                            var DB05 = new byte[_DB05Len];
                            var DB05ReadResult = _s7Client.DBRead(_DB05Num, 0, _DB05Len, DB05);

                            if (DB05ReadResult != 0)
                            {
                                _isConnected = false;
                                PLCConnState = false;
                                SaveErrToText("DB5读失败", $"ErrorCode:{DB05ReadResult}");
                                continue;   // 原为 return，改为 continue 保持循环
                            }

                            string DB5Name = $"{_ConveIP}:DB5";
                            PLCDB5Dic[DB5Name] = ByteToString(DB05);

                            byte[] MessageTypeBytes = new byte[2],
                                   StUintIDBytes = new byte[30],
                                   FromLocationBytes = new byte[4],
                                   ToLacationBytes = new byte[4],
                                   StUnitHeightBytes = new byte[4],
                                   StUnitWeightBytes = new byte[6],
                                   ReasonCodeBytes = new byte[8],
                                   CanWriteBytes = new byte[2];

                            Array.Copy(DB05, 2, MessageTypeBytes, 0, 2);
                            Array.Copy(DB05, 6, StUintIDBytes, 0, 30);
                            Array.Copy(DB05, 38, FromLocationBytes, 0, 4);
                            Array.Copy(DB05, 44, ToLacationBytes, 0, 4);
                            Array.Copy(DB05, 50, StUnitHeightBytes, 0, 4);
                            Array.Copy(DB05, 56, StUnitWeightBytes, 0, 6);
                            Array.Copy(DB05, 64, ReasonCodeBytes, 0, 8);
                            Array.Copy(DB05, 74, CanWriteBytes, 0, 2);

                            MessageTypeBytes = ClearNullChar(MessageTypeBytes.ToList());
                            StUintIDBytes = ClearNullChar(StUintIDBytes.ToList());
                            FromLocationBytes = ClearNullChar(FromLocationBytes.ToList());
                            ToLacationBytes = ClearNullChar(ToLacationBytes.ToList());
                            StUnitHeightBytes = ClearNullChar(StUnitHeightBytes.ToList());
                            StUnitWeightBytes = ClearNullChar(StUnitWeightBytes.ToList());
                            ReasonCodeBytes = ClearNullChar(ReasonCodeBytes.ToList());
                            CanWriteBytes = ClearNullChar(CanWriteBytes.ToList());

                            string MessageType = Encoding.ASCII.GetString(MessageTypeBytes);
                            string StUintID = Encoding.ASCII.GetString(StUintIDBytes);
                            string FromLocation = Encoding.ASCII.GetString(FromLocationBytes);
                            string ToLacation = Encoding.ASCII.GetString(ToLacationBytes);
                            string StUnitHeight = Encoding.ASCII.GetString(StUnitHeightBytes);
                            string StUnitWeight = Encoding.ASCII.GetString(StUnitWeightBytes);
                            string ReasonCode = Encoding.ASCII.GetString(ReasonCodeBytes);
                            string CanWrite = Encoding.ASCII.GetString(CanWriteBytes);

                            if (CanWrite == "01")
                            {
                                byte[] data = CreatSubByets("10").ToArray();

                                if (!_isConnected)
                                {
                                    SaveLogText("Recv", "写入前连接已断开，跳过本次回复");
                                    continue;
                                }

                                var writeResult = _s7Client.DBWrite(_DB05Num, 72, 4, data);
                                if (writeResult != 0)
                                {
                                    _isConnected = false;
                                    PLCConnState = false;
                                    SaveErrToText("DB5写回复失败", $"ErrorCode:{writeResult}");
                                    continue;   // 原为 return，改为 continue
                                }

                                string hexString = BitConverter.ToString(data).Replace("-", " ");
                                SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5",
                                    $"已写入CanWriter: {hexString}, ASCII: {Encoding.ASCII.GetString(data)}");

                                #region 业务逻辑（完全保留，未改变）
                                if (MessageType == "ET")
                                {
                                    string status = "";
                                    if (FromLocation == "6005") status = "F1-RETURN-02";
                                    else if (FromLocation == "6002") status = "F1-RETURN-01";
                                    else if (FromLocation == "6008") status = "F1-RETURN-03";
                                    ToTesApiService.releaseStation(status);
                                    SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5", $"ET命令：站点{FromLocation}");
                                }
                                else if (MessageType == "RF")
                                {
                                    FromLocation = DeleteChar(FromLocation);
                                    ReasonCode = DeleteChar(ReasonCode);
                                    TessetStationStatus stationStatus = new TessetStationStatus();
                                    stationStatus.stationCode = FromLocation;
                                    // nan_T 2026-09-09：使用合并后的统一服务实例
                                    toTesApiService.setStationStatus(stationStatus);
                                    SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5", $"RF命令：站点{FromLocation}，流向切换成功");
                                }
                                else if (MessageType == "SS")
                                {
                                    string dpjname = "", stationname = "";
                                    switch (FromLocation)
                                    {
                                        case "6010": dpjname = "DPJ-01"; stationname = "F1-RETURN-01"; break;
                                        case "6011": dpjname = "DPJ-02"; stationname = "F1-RETURN-02"; break;
                                        case "6012": dpjname = "DPJ-03"; stationname = "F1-RETURN-03"; break;
                                        default:
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5", $"未知站点 {FromLocation}，忽略SS指令");
                                            continue;
                                    }

                                    // 使用站点专属锁，保证检查与设置的原子性
                                    var lockObj = _stationLocks.GetOrAdd(dpjname, new object());
                                    bool shouldApply = false;

                                    lock (lockObj)
                                    {
                                        // 1. 检查是否存在未完成的叠盘任务（两种状态分别查询）
                                        var taskPending = TaskDbContext.GetByTesstationCodeTaskTypeAsync(stationname, "FOLD", "待执行");
                                        var taskRunning = TaskDbContext.GetByTesstationCodeTaskTypeAsync(stationname, "FOLD", "开始执行任务");
                                        if (taskPending != null || taskRunning != null)
                                        {
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5",
                                                $"站点 {dpjname} 已有叠盘任务（待执行或执行中），忽略SS");
                                            continue;   // 离开 lock 块，自动释放锁
                                        }

                                        // 2. 冷却时间检查
                                        if (_lastEmptyPalletApplyTime.TryGetValue(dpjname, out DateTime lastTime) &&
                                            (DateTime.UtcNow - lastTime).TotalSeconds < EMPTY_PALLET_COOLDOWN_SECONDS)
                                        {
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5",
                                                $"站点 {dpjname} SS指令过于频繁（{EMPTY_PALLET_COOLDOWN_SECONDS}s内），忽略");
                                            continue;
                                        }

                                        // 3. 检查是否已有申请进行中
                                        if (_isApplyingEmptyPallet.TryGetValue(dpjname, out bool applying) && applying)
                                        {
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5",
                                                $"站点 {dpjname} 已有申请进行中，忽略");
                                            continue;
                                        }

                                        // 通过所有检查，更新状态并允许申请
                                        _isApplyingEmptyPallet[dpjname] = true;
                                        _lastEmptyPalletApplyTime[dpjname] = DateTime.UtcNow;
                                        shouldApply = true;
                                    }

                                    // 执行申请（在锁外进行，避免长时间占用锁）
                                    if (shouldApply)
                                    {
                                        try
                                        {
                                            UpdateEmptyPallet emptyPallet = new UpdateEmptyPallet();
                                            UpdateEmptyPalletData emptyPalletData = new UpdateEmptyPalletData();
                                            emptyPalletData.carrierLoc = dpjname;
                                            emptyPallet.strInMsg = JsonConvert.SerializeObject(emptyPalletData);
                                            toTesApiService.PostPalnoApply(emptyPallet);
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5",
                                                $"SS命令：站点{dpjname}，申请空托盘垛成功");
                                        }
                                        catch (Exception ex)
                                        {
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB5",
                                                $"站点 {dpjname} 申请异常: {ex.Message}");
                                        }
                                        finally
                                        {
                                            // 释放申请中标记
                                            _isApplyingEmptyPallet[dpjname] = false;
                                        }
                                    }
                                }
                                #endregion
                            }
                        }

                        // ---- DB6 ----
                        if (_DB06Num > 0 && _DB06Len > 0)
                        {
                            var DB06 = new byte[_DB06Len];
                            var DB06ReadResult = _s7Client.DBRead(_DB06Num, 2, _DB06Len, DB06);

                            if (DB06ReadResult != 0)
                            {
                                _isConnected = false;
                                PLCConnState = false;
                                SaveErrToText("DB6读失败", $"ErrorCode:{DB06ReadResult}");
                                continue;   // 原为 return，改为 continue
                            }

                            string DB6Name = $"{_ConveIP}:DB6";
                            PLCDB5Dic[DB6Name] = ByteToString(DB06);
                            List<DPJConveVisu> dPJConveVisus = ParseStationData(DB06);

                            for (int i = 0; i < dPJConveVisus.Count; i++)
                            {
                                EmptyPodIDApply temp = TaskDbContext.GetEmptyPodIDApplyByPodID(dPJConveVisus[i].Podid);
                                if (!dPJConveVisus[i].Podid.Contains("EMPTY"))
                                {
                                    if (temp == null)
                                    {
                                        if (dPJConveVisus[i].FoldNum == "10" && dPJConveVisus[i].Podid.Contains("XTEK"))
                                        {
                                            string dpjname = "";
                                            switch (dPJConveVisus[i].DeviceID)
                                            {
                                                case "6010": dpjname = "6003"; break;
                                                case "6011": dpjname = "6006"; break;
                                                case "6012": dpjname = "6009"; break;
                                            }
                                            ConveSendMessCollection.Add(new ConveSendMess
                                            {
                                                MessType = "TT",
                                                PLCIP = "172.18.18.20",
                                                FromLocation = dPJConveVisus[i].DeviceID,
                                                ToLocation = dpjname,
                                                StUintID = dPJConveVisus[i].Podid,
                                                StUnitHeight = "",
                                                StUnitWeight = "",
                                                CanWrite = "01",
                                            });
                                            EmptyPodIDApply emptyPodIDApply = new EmptyPodIDApply
                                            {
                                                PodID = dPJConveVisus[i].Podid,
                                                PodNum = Convert.ToInt32(dPJConveVisus[i].FoldNum)
                                            };
                                            TaskDbContext.InsertAddEmptyPodIDApply(emptyPodIDApply);
                                            SaveLogText($"{_ConveInfo.PLCSymbol}:Recv—DB6", $"{dPJConveVisus[i].Podid}/{dPJConveVisus[i].DeviceID},叠满开始入库");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    SaveErrToText($"{_ConveInfo.PLCSymbol}:Recv", $"{err.Message}/{err.StackTrace}");
                    lock (_clientLock)
                    {
                        _s7Client.Disconnect();
                        _isConnected = false;
                        PLCConnState = false;
                    }
                }

                await Task.Delay(100);
            }
        }

        // nan_T 2026-09-09：async void → async Task，异常可被 Task 观测而不是直接崩溃进程
        async Task Send()
        {
            while (true)
            {
                try
                {
                    if (!EnsureConnection())
                    {
                        await Task.Delay(_retryDelaySend);
                        _retryDelaySend = Math.Min(_retryDelaySend * 2, MAX_RETRY_DELAY);
                        continue;
                    }
                    _retryDelaySend = 1000;

                    lock (_clientLock)
                    {
                        if (_DB04Num > 0 && _DB04Len > 0)
                        {
                            var DB04 = new byte[_DB04Len];
                            var DB04ReadResult = _s7Client.DBRead(_DB04Num, 0, _DB04Len, DB04);

                            if (DB04ReadResult != 0)
                            {
                                _isConnected = false;
                                PLCConnState = false;
                                SaveErrToText("DB4读失败", $"ErrorCode:{DB04ReadResult}");
                                continue;   // 原为 return，改为 continue
                            }

                            byte[] LastByte = new byte[2];
                            Array.Copy(DB04, _DB04Len - 2, LastByte, 0, 2);
                            string lastbytestr = Encoding.ASCII.GetString(LastByte);
                            if (lastbytestr == "10")
                            {
                                var datalist = ConveSendMessCollection.Refesh();
                                var SendMessList = datalist.FindAll(x => x.PLCIP == _ConveIP);

                                if (SendMessList != null && SendMessList.Any())
                                {
                                    var ConveSendMess = SendMessList.First();
                                    if (ConveSendMess != null)
                                    {
                                        byte[] SendMess = CreatTaskMess(ConveSendMess);
                                        var ByteList = SendMess.ToList();
                                        SendMess = ByteList.ToArray();

                                        if (SendMess.Length == _DB04Len)
                                        {
                                            if (!_isConnected)
                                            {
                                                SaveLogText("Send", "写入DB4前连接已断开，跳过");
                                                continue;
                                            }

                                            var sendResult = _s7Client.DBWrite(_DB04Num, 0, _DB04Len, SendMess);
                                            if (sendResult != 0)
                                            {
                                                _isConnected = false;
                                                PLCConnState = false;
                                                SaveErrToText("DB4写失败", $"ErrorCode:{sendResult}");
                                                continue;   // 原为 return，改为 continue
                                            }
                                            else
                                            {
                                                string MessDesc = $"MessType:{ConveSendMess.MessType} / Station:{ConveSendMess.ToLocation} / BoxID:{ConveSendMess.StUintID} / StuType:{ConveSendMess.StUnitHeight} / CanWriter:{ConveSendMess.CanWrite}";
                                                SaveLogText(MessDesc, "SEND");
                                                try
                                                {
                                                    _ConveEvent.OnShowSend(new ConveMessageEventArgs(_ConveIP + "=>  [" + MessDesc + "]      " + DateTime.UtcNow.ToString()));
                                                }
                                                catch { }
                                            }
                                        }
                                        ConveSendMessCollection.Remove(ConveSendMess.DataGuid);
                                    }
                                }
                                else
                                {
                                    // ★ 心跳：改为读取 DB4 前 2 个字节（保持连接活跃）
                                    if (_isConnected && _DB04Len >= 2)
                                    {
                                        byte[] dummy = new byte[2];
                                        var readResult = _s7Client.DBRead(_DB04Num, 0, 2, dummy);
                                        if (readResult != 0)
                                        {
                                            _isConnected = false;
                                            PLCConnState = false;
                                            SaveErrToText("DB4心跳读失败", $"ErrorCode:{readResult}");
                                            continue;   // 原为 return，改为 continue
                                        }
                                    }
                                    // 如果长度不足，记录日志但不报错
                                    else
                                    {
                                        SaveLogText("Send", "DB4长度不足，跳过心跳读取");
                                    }
                                }
                            }
                            else
                            {
                                // ★ 心跳：同样改为读取
                                if (_isConnected && _DB04Len >= 2)
                                {
                                    byte[] dummy = new byte[2];
                                    var readResult = _s7Client.DBRead(_DB04Num, 0, 2, dummy);
                                    if (readResult != 0)
                                    {
                                        _isConnected = false;
                                        PLCConnState = false;
                                        SaveErrToText("DB4心跳读失败", $"ErrorCode:{readResult}");
                                        continue;   // 原为 return，改为 continue
                                    }
                                }
                                else
                                {
                                    SaveLogText("Send", "DB4长度不足，跳过心跳读取");
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    SaveErrToText($"{_ConveInfo.PLCSymbol}:Send", $"{err.Message}/{err.StackTrace}");
                    lock (_clientLock)
                    {
                        _s7Client.Disconnect();
                        _isConnected = false;
                        PLCConnState = false;
                    }
                }

                await Task.Delay(100);
            }
        }

        // ========== 以下辅助方法保持不变 ==========
        public static List<DPJConveVisu> ParseStationData(byte[] rawData)
        {
            if (rawData == null || rawData.Length < 138)
                throw new ArgumentException("数据长度至少需要138字节");

            const int stationDataLen = 42;
            const int stationSize = 14;
            const int podidBlockSize = 32;

            int stationCount = stationDataLen / stationSize;
            var result = new List<DPJConveVisu>(stationCount);

            for (int i = 0; i < stationCount; i++)
            {
                int stationOffset = i * stationSize;
                string deviceId = ExtractString(rawData, stationOffset, 4);
                string deviceStatus = ExtractString(rawData, stationOffset + 4, 2);
                string foldNum = ExtractString(rawData, stationOffset + 6, 2);
                string mode = ExtractString(rawData, stationOffset + 8, 2);
                string foldReady = ExtractString(rawData, stationOffset + 10, 2);

                int podidOffset = stationDataLen + i * podidBlockSize;
                string podId = ExtractString(rawData, podidOffset, podidBlockSize);

                result.Add(new DPJConveVisu
                {
                    DeviceID = deviceId,
                    DeviceStatus = deviceStatus,
                    FoldNum = foldNum.ToString(),
                    Mode = mode,
                    FoldReady = foldReady,
                    Podid = podId
                });
            }
            return result;
        }

        private static string ExtractString(byte[] data, int offset, int length)
        {
            if (offset + length > data.Length)
                throw new ArgumentOutOfRangeException("偏移或长度超出数组范围。");

            string raw = Encoding.ASCII.GetString(data, offset, length);
            string trimmed = raw.TrimEnd('\0', ' ');
            string noStar = trimmed.Replace("*", "");
            var validChars = noStar.Where(c => c >= 32 && c <= 126).ToArray();
            return new string(validChars);
        }

        void SaveErrToText(string Detail, string reason)
        {
            const string FilePath = @"C:\WCSErrLog";
            var FileName = DateTime.UtcNow.ToString("yyyyMMdd");

            StreamWriter sw = null;
            FileStream fs = null;

            try
            {
                if (!Directory.Exists(FilePath))
                    Directory.CreateDirectory(FilePath);

                FileName = "WcsErrLog_" + FileName + ".txt";
                var Path = FilePath + @"\" + FileName;

                if (!File.Exists(Path))
                {
                    fs = File.Create(Path);
                    fs.Close();
                    fs.Dispose();
                }

                sw = File.AppendText(Path);
                var AddTxt = $"DealMessage:{Detail} \n Reason:{reason} \n Datetime:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} \n";
                sw.Write(AddTxt);
                sw.Close();
            }
            catch
            {
                sw?.Close();
                fs?.Close();
            }
        }

        public string DeleteChar(string str)
        {
            string temp = "";
            for (int i = 0; i < str.Length; i++)
            {
                char C = str[i];
                if (C != '*')
                    temp += C;
            }
            return temp;
        }

        byte[] CreatTaskMess(ConveSendMess _SendMess)
        {
            var SendBytes = new List<byte>();
            SendBytes.AddRange(CreatSubByets(_SendMess.MessType.PadLeft(2, '0')));
            SendBytes.AddRange(CreatSubByets(_SendMess.StUintID.PadLeft(30, '*')));
            SendBytes.AddRange(CreatSubByets(_SendMess.FromLocation.PadLeft(4, '0')));
            SendBytes.AddRange(CreatSubByets(_SendMess.ToLocation.PadLeft(4, '0')));
            SendBytes.AddRange(CreatSubByets(_SendMess.StUnitHeight.PadLeft(4, '0')));
            SendBytes.AddRange(CreatSubByets(_SendMess.StUnitWeight.PadLeft(6, '0')));
            SendBytes.AddRange(CreatSubByets(_SendMess.ReasonCode.PadLeft(8, '0')));
            SendBytes.AddRange(CreatSubByets(_SendMess.CanWrite));
            return SendBytes.ToArray();
        }

        List<byte> CreatSubByets(string Data)
        {
            var Bytes = new List<byte>();
            var DataLen = Data.Length;
            Bytes.Add((byte)DataLen);
            Bytes.Add((byte)DataLen);
            byte[] DataByte = Encoding.ASCII.GetBytes(Data);
            Bytes.AddRange(DataByte);
            return Bytes;
        }

        byte[] ClearNullChar(List<byte> RecvByteList)
        {
            IEnumerable<byte> bytelist = from t in RecvByteList where t > 31 && t < 126 select t;
            return bytelist.ToArray();
        }

        string ByteToString(byte[] bytes)
        {
            int IndexNum = 0;
            string result = "";
            foreach (var Item in bytes)
            {
                result += $"{IndexNum}={Item} ";
                IndexNum++;
            }
            return result.TrimEnd(' ');
        }

        void SaveLogText(string Detail, string MessType)
        {
            const string FilePath = @"C:\PLCLog";
            var FileName = DateTime.UtcNow.ToString("yyyyMMdd_HH");

            StreamWriter sw = null;
            FileStream fs = null;

            try
            {
                if (!Directory.Exists(FilePath))
                    Directory.CreateDirectory(FilePath);

                FileName = $"{_ConveSymbol}_Log{FileName}.txt";
                var Path = FilePath + @"\" + FileName;

                if (!File.Exists(Path))
                {
                    fs = File.Create(Path);
                    fs.Close();
                    fs.Dispose();
                }

                sw = File.AppendText(Path);
                var AddTxt = $"{MessType}=>{Detail}  {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} \n";
                sw.Write(AddTxt);
                sw.Close();
            }
            catch
            {
                sw?.Close();
                fs?.Close();
            }
        }

        string getWMSTaskID()
        {
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string guidPart = Guid.NewGuid().ToString("N").Substring(0, 4);
            return $"WCS{timestamp}{guidPart}";
        }
    }
}