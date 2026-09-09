using WCS_Models;
using WCS_Models.PLCModel;
using WCS_Models.SqlModel;
using WCS_Models.TESModel;
using WCS_Models.WCSModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Helper.Mapper;
using Newtonsoft.Json;
//using S7.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCS_Helper;
using WCS_Models.WMSModel;

namespace WCS_Services
{
    public class SeverService
    {
    }

    public class AgvPalletStationCollection
    {
        public static List<AgvPalletStation> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }



        const string TableName = "AgvPalletStation";// 数据库表名称

        static List<AgvPalletStation> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象
        static readonly object _addLock = new object();


        static List<AgvPalletStation> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<AgvPalletStation>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<AgvPalletStation>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }
        public static void Update(AgvPalletStation New_Data)
        {
            lock (_DataLocker)
            {
                if (New_Data == null) return;

                var Ago_Info = DataList?.Find(x => x.Id == New_Data.Id);// 查找待更新的数据

                if (Ago_Info == null) return;

                int count = WcsSQLServer.ExecuteUpdate(TableName, Ago_Info, New_Data);// 执行数据库更新操作

                // 若成功更新，则从数据列表中移除旧数据，添加新数据
                if (count > 0)
                {
                    _DataList.Remove(Ago_Info);

                    _DataList.Add(New_Data);
                }
            }
        }
    }

    public class ConveInfoColleciton
    {
        public static List<ConveInfo> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }



        const string TableName = "ConveInfo";// 数据库表名称

        static List<ConveInfo> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象

        static List<ConveInfo> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {                
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<ConveInfo>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<ConveInfo>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }
    }

    // 用于存储传送消息的集合类
    public class ConveSendMessCollection
    {
        // 数据列表属性
        public static List<ConveSendMess> DataList
        {
            get
            {
                return Initialization();
            }
        }

        static List<ConveSendMess> _DataList;// 数据列表

        const string TableName = "ConveSendMess";// 数据表名

        readonly static object _Locker = new object();// 对象锁
        readonly static object _DataLocker = new object();// 数据锁
        static readonly object _addLock = new object();

        // 初始化数据列表
        static List<ConveSendMess> Initialization()
        {
            lock (_Locker)
            {
                if (_DataList == null)
                    _DataList = WcsSQLServer.SelectContent<ConveSendMess>(TableName);// 若数据列表为空，则从数据库中加载数据
                if (_DataList == null)
                    _DataList = new List<ConveSendMess>();// 若数据库中无数据，则创建空列表
                return _DataList;
            }
        }

        //刷新数据表
        public static List<ConveSendMess> Refesh()
        {
            lock (_Locker)
            {
                _DataList = WcsSQLServer.SelectContent<ConveSendMess>(TableName);// 从数据库中加载数据
                return _DataList;
            }
        }


        // 添加传送消息

        //public static void Add(ConveSendMess SendMess)
        //{
        //        if (SendMess == null) return;

        //        string BoxID = SendMess.StUintID;

        //        var _SendMess = DataList.Find(x => x.StUintID == BoxID);// 查找是否已存在相同箱子ID的消息，若存在则不添加

        //        if (_SendMess != null) return;

        //        int count = TaskDbContext.ExecuteInsert(TableName, SendMess);// 向数据库插入数据

        //        if (count > 0) _DataList.Add(SendMess);// 若成功插入数据库，则添加到数据列表
        //                                               // 记录插入操作开始
        //        var startLog = new LogModel
        //        {
        //            UserType = "database",
        //            LogType = "Database Insert",
        //            Level = "Info",
        //            Message = $"开始向{TableName}数据列表插入数据",
        //            Module = "TaskDbContext",
        //            Operation = "ExecuteInsert",
        //            Details = $"数据列表表名: {TableName}\n插入数据: {JsonConvert.SerializeObject(SendMess)}",
        //            UserId = "System",
        //            IpAddress = "System",
        //            CreateTime = DateTime.UtcNow,
        //            IsArchived = "False"
        //        };
        //        LogsDbContext.Insert(startLog);

        //}

        // 移除传送消息
        public static void Add(ConveSendMess SendMess)
        {
            if (SendMess == null) return;

            lock (_addLock)
            {
                try
                {
                    string BoxID = SendMess.StUintID;

                    var _SendMess = DataList.Find(x => x.StUintID == BoxID);

                    if (_SendMess != null) return;
                    int count = WcsSQLServer.ExecuteInsert(TableName, SendMess);

                    if (count > 0)
                    {
                        _DataList.Add(SendMess);
                    }
                }
                catch (Exception ex)
                {
                    // 记录异常日志
                    var errorLog = new LogModel
                    {
                        UserType = "system",
                        LogType = "Add Method Error",
                        Level = "Error",
                        Message = $"Add方法执行失败: {ex.Message}",
                        Module = "ConveSendMessCollection",
                        Operation = "Add",
                        Details = $"异常详情: {ex}",
                        UserId = "System",
                        IpAddress = "System",
                        CreateTime = DateTime.UtcNow,
                        IsArchived = "False"
                    };
                    LogsDbContext.Insert(errorLog);
                }
            }
        }
        public static void Remove(string DataGuid)
        {
            lock (_DataLocker)
            {
                var _SendMess = DataList?.Find(x => x.DataGuid == DataGuid);// 查找待移除的传送消息

                if (_SendMess == null) return;

                int count = WcsSQLServer.ExecuteDelete(TableName,
                    new Dictionary<string, string>
                    {
                    { "DataGuid", DataGuid }
                    });// 从数据库删除数据

                if (count > 0) _DataList.Remove(_SendMess);// 若成功从数据库删除，则从数据列表中移除
            }
        }
    }

    // 用于存储传送可视化数据的集合类
    public class ConveVisuCollection
    {
        // 数据列表属性
        public static List<ConveVisu> DataList
        {
            get
            {
                return Initialization();
            }
        }

        static readonly object _Locker = new object();// 对象锁
        static readonly object _DataLocker = new object();// 数据锁

        static List<ConveVisu> _DataList;//数据列表

        const string TableName = "ConveVisu";//数据表名

        public static int CarVisuPos = 0;//车辆可视位置

        //初始化数据列表
        static List<ConveVisu> Initialization()
        {
            lock (_Locker)
            {
                if (_DataList == null)
                    //_DataList = WcsSQLServer.SelectContent<ConveVisu>(TableName);// 若数据列表为空，则从数据库中加载数据
                if (_DataList == null)
                    _DataList = new List<ConveVisu>();// 若数据库中无数据，则创建空列表
                return _DataList;
            }
        }

        //添加数据
        public static int Add(ConveVisu Data)
        {
            lock (_DataLocker)
            {
                if (Data == null) return 0;

                var Check_Data = DataList.Find(x => x.PLCIP == Data.PLCIP && x.DeviceNum == Data.DeviceNum);// 检查是否已存在相同的 PLCIP 和 DeviceNum 的数据

                if (Check_Data != null) return 0;

                var LastData = DataList.FindAll(x => x.PLCIP == Data.PLCIP).OrderByDescending(x => x.IndexNum).FirstOrDefault();// 获取同一 PLCIP 下的最后一条数据的索引编号

                if (LastData == null)
                {
                    Data.IndexNum = 0;
                }
                else
                {
                    int IndexNum = LastData.IndexNum + 1;

                    Data.IndexNum = IndexNum;
                }

                int count = WcsSQLServer.ExecuteInsert(TableName, Data);// 向数据库插入数据

                if (count > 0) _DataList.Add(Data);// 若成功插入数据库，则添加到数据列表

                return count;
            }
        }

        //移除数据
        public static void Remove(string DataGuid)
        {
            lock (_DataLocker)
            {
                var Check_Data = DataList.Find(x => x.DataGuid == DataGuid);//查找待移除的数据

                if (Check_Data == null) return;

                int count = WcsSQLServer.ExecuteDelete(TableName,
                new Dictionary<string, string>
                {
                { "DataGuid", DataGuid }
                });// 从数据库删除数据

                if (count > 0) _DataList.Remove(Check_Data); // 若成功从数据库删除，则从数据列表中移除
            }
        }

        //更新数据
        public static void Update(ConveVisu New_Data)
        {
            lock (_DataLocker)
            {
                if (New_Data == null) return;

                var Ago_Info = DataList?.Find(x => x.DataGuid == New_Data.DataGuid);// 查找待更新的数据

                if (Ago_Info == null) return;

                int count = WcsSQLServer.ExecuteUpdate(TableName, Ago_Info, New_Data);// 执行数据库更新操作

                // 若成功更新，则从数据列表中移除旧数据，添加新数据
                if (count > 0)
                {
                    _DataList.Remove(Ago_Info);

                    _DataList.Add(New_Data);
                }
            }
        }

        // 更新可视化数据
        public static void UpdateVisuData(string PLCIP, byte[] VisuData)
        {
            // 清除非ASCII字符的方法
            byte[] ClearNullChar(byte[] byteArray)
            {
                // 使用更高效的方式过滤
                return byteArray.Where(b => b >= 32 && b <= 126).ToArray();
            }

            lock (_DataLocker)
            {
                int blockSize = 28; // 每个数据块大小
                int blockCount = VisuData.Length / blockSize;

                for (int i = 0; i < blockCount; i++)
                {
                    int offset = i * blockSize; // 当前块的起始位置

                    // 从VisuData中提取指定位置的数据
                    byte[] statusBytes = new byte[2];
                    byte[] fromLocationBytes = new byte[14];
                    byte[] podIdBytes = new byte[8];

                    // 正确计算偏移量
                    Array.Copy(VisuData, offset, statusBytes, 0, 2);
                    Array.Copy(VisuData, offset + 6, fromLocationBytes, 0, 14);
                    Array.Copy(VisuData, offset + 20, podIdBytes, 0, 8);

                    // 过滤非打印字符
                    statusBytes = ClearNullChar(statusBytes);
                    fromLocationBytes = ClearNullChar(fromLocationBytes);
                    podIdBytes = ClearNullChar(podIdBytes);

                    // 转换为字符串
                    string status = Encoding.ASCII.GetString(statusBytes);
                    string fromLocation = Encoding.ASCII.GetString(fromLocationBytes);
                    string podId = Encoding.ASCII.GetString(podIdBytes);

                    // 查找并更新数据
                    var existingData = DataList.Find(x => x != null && x.IndexNum == i && x.PLCIP == PLCIP);

                    if (existingData != null)
                    {
                        existingData.Status = status[0]; // 使用转换后的字符串
                        existingData.DeviceNum = fromLocation;
                        existingData.Podid = podId;
                    }
                    //TaskDbContext.UpdateConvevisuStatusAsync(existingData);// 若数据存在，则更新状态
                }
            }
        }
    }

    public class ErrorPalletForPLCCollection
    {
        public static List<ErrorPalletForPLC> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }

        const string TableName = "ErrorPalletForPLC";// 数据库表名称

        static List<ErrorPalletForPLC> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象

        static List<ErrorPalletForPLC> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<ErrorPalletForPLC>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<ErrorPalletForPLC>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }

        public static void Add(ErrorPalletForPLC SendMess)
        {
            lock (_DataLocker)
            {
                if (SendMess == null) return;

                string Podid = SendMess.StUintID;

                var _SendMess = DataList.Find(x => x.StUintID == Podid);// 查找是否已存在相同箱子ID的消息，若存在则不添加

                if (_SendMess != null) return;

                int count = WcsSQLServer.ExecuteInsert(TableName, SendMess);// 向数据库插入数据

                if (count > 0) _DataList.Add(SendMess);// 若成功插入数据库，则添加到数据列表
            }
        }

        public static void Remove(ErrorPalletForPLC _SendMess)
        {
            lock (_DataLocker)
            {
                if (_SendMess == null) return;

                int count = WcsSQLServer.ExecuteDelete(TableName,
                    new Dictionary<string, string>
                    {
                    { "StUintID", _SendMess.StUintID }
                    });// 从数据库删除数据

                if (count > 0) _DataList.Remove(_SendMess);// 若成功从数据库删除，则从数据列表中移除
            }
        }
    }

    public class LogModelCollection
    {

        const string TableName = "SystemLogs";// 数据库表名称

        public static void ClearLog()
        {
            var DataList = WcsSQLServer.SelectContent<LogModel>(TableName);
            var LogList = DataList.FindAll(x => (DateTime.UtcNow - Convert.ToDateTime(x.CreateTime)).Days > 30);

            if (LogList.Any())
            {
                LogList.ForEach(x =>
                {
                    string id = x.Id.ToString();

                    int count = WcsSQLServer.ExecuteDelete(TableName,
                    new Dictionary<string, string>
                    {
                        { "id", id }
                    });

                });
            }
        }
    }

    public class TescorrespondsWmsCollection
    {
        const string TableName = "SystemLogs";// 数据库表名称

        public static void ClearLog()
        {
            var DataList = WcsSQLServer.SelectContent<TescorrespondsWms>(TableName);
            var LogList = DataList.FindAll(x => (DateTime.UtcNow - Convert.ToDateTime(x.CreationTime)).Days > 30);

            if (LogList.Any())
            {
                LogList.ForEach(x =>
                {
                    string id = x.id.ToString();

                    int count = WcsSQLServer.ExecuteDelete(TableName,
                    new Dictionary<string, string>
                    {
                        { "id", id }
                    });

                });
            }
        }
    }

    public class AislesCollection
    {
        public static List<Aisles> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }

        const string TableName = "Aisles";// 数据库表名称

        static List<Aisles> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象

        static List<Aisles> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<Aisles>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<Aisles>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }       
    }

    public class StoragelocationCollection
    {
        public static List<Storagelocation> DataList
        {
            get
            {
                return Initialization();
            }
        }

        const string TableName = "jx_storagelocation";
        static List<Storagelocation> _DataList;
        static readonly object _Locker = new object();
        static readonly object _DataLocker = new object();

        static List<Storagelocation> Initialization()
        {
            lock (_Locker)
            {
                if (_DataList == null)
                    _DataList = WcsSQLServer.SelectContent<Storagelocation>(TableName);
                if (_DataList == null)
                    _DataList = new List<Storagelocation>();
                return _DataList;
            }
        }

        /// <summary>
        /// 清空所有存储点位的容器号（数据库和内存缓存）
        /// </summary>
        public static void ClearAllPodIDs()
        {
            lock (_DataLocker)
            {
                string sql = $"UPDATE {TableName} SET podid = NULL";
                int count = WcsSQLServer.Execute(sql);

                if (count > 0)
                {
                    if (_DataList != null)
                    {
                        foreach (var loc in _DataList)
                        {
                            loc.PodID = null;
                        }
                    }
                }
                else
                {
                    WcsSQLServer.SaveErrToText($"ClearAllPodIDs: 影响行数为 0，SQL: {sql}", "清空失败或表为空");
                }
            }
        }

        /// <summary>
        /// 更新存储点位数据（按 Location 匹配，使用字典方式指定列名）
        /// </summary>
        public static void Update(Storagelocation newData)
        {
            lock (_DataLocker)
            {
                if (newData == null) return;

                // 查找旧数据（假设 Location 唯一）
                var oldData = DataList?.Find(x => x.Location == newData.Location);
                if (oldData == null)
                {
                    WcsSQLServer.SaveErrToText($"Update: Location '{newData.Location}' not found in cache", "Old data missing");
                    return;
                }

                // 使用字典方式更新，明确指定数据库列名（小写）
                var whereParams = new Dictionary<string, string>
                {
                    { "location", newData.Location }   // 数据库列名（如有差异请调整）
                };
                var updateParams = new Dictionary<string, string>
                {
                    { "podid", newData.PodID }         // 数据库列名
                };

                int count = WcsSQLServer.ExecuteUpdate(TableName, whereParams, updateParams);
                if (count > 0)
                {
                    _DataList.Remove(oldData);
                    _DataList.Add(newData);
                }
                else
                {
                    WcsSQLServer.SaveErrToText($"Update returned 0 for Location: {newData.Location}, PodID: {newData.PodID}", "No rows updated");
                }
            }
        }
    }

    public class StationNameModelCollection
    {
        public static List<StationNameModel> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }

        const string TableName = "StationName";// 数据库表名称

        static List<StationNameModel> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象

        static List<StationNameModel> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<StationNameModel>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<StationNameModel>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }

    }

    public class StationstatusCollection
    {
        public static List<Stationstatus> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }

        const string TableName = "Stationstatus";// 数据库表名称

        static List<Stationstatus> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象

        static List<Stationstatus> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<Stationstatus>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<Stationstatus>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }

    }

    /// <summary>
    /// 查询API地址
    /// </summary>
    public class ApiServerCollection
    {
        public static List<ApiServer> DataList
        {
            get
            {
                return Initialization();// 返回数据列表
            }
        }

        const string TableName = "ApiServer";// 数据库表名称

        static List<ApiServer> _DataList;// 数据列表

        static readonly object _Locker = new object();// 用于初始化期间的线程安全锁对象
        static readonly object _DataLocker = new object();// 用于操作数据期间的线程安全锁对象

        static List<ApiServer> Initialization()
        {
            // 使用锁以确保初始化期间的线程安全性
            lock (_Locker)
            {
                if (_DataList == null)// 检查列表是否为 null
                    _DataList = WcsSQLServer.SelectContent<ApiServer>(TableName);// 从 SQL Server 中检索数据
                if (_DataList == null)// 如果仍为 null，则初始化为空列表
                    _DataList = new List<ApiServer>();// 若数据库中无数据，则创建空列表
                return _DataList;// 返回初始化后的列表
            }
        }

    }
}

