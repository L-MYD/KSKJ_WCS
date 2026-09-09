using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using WCS_Helper.Mapper;
using WCS_Models.WCSModel.LogModel;

namespace WCS_Helper
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// 日志配置类
    /// </summary>
    public class LogConfiguration
    {
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableDatabaseLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFileCount { get; set; } = 10;
    }

    /// <summary>
    /// 高性能日志辅助类（支持异步批量写入）
    /// </summary>
    public static class LogHelper
    {
        private static readonly LogConfiguration _config = new LogConfiguration();
        private static readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private static readonly ManualResetEventSlim _logEvent = new ManualResetEventSlim(false);
        private static readonly Thread _logThread;
        private static volatile bool _isRunning = true;
        private static readonly object _configLock = new object();

        static LogHelper()
        {
            // 初始化日志线程
            _logThread = new Thread(ProcessLogQueue)
            {
                Name = "LogProcessor",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _logThread.Start();

            // 注册应用程序退出事件
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

            // 确保日志目录存在
            EnsureLogDirectory();
        }

        /// <summary>
        /// 配置日志设置
        /// </summary>
        public static void Configure(Action<LogConfiguration> configureAction)
        {
            lock (_configLock)
            {
                configureAction?.Invoke(_config);
            }
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void Debug(string module, string operation, string message, string details = null)
        {
            WriteLog(module, operation, message, LogLevel.Debug, details);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string module, string operation, string message, string details = null)
        {
            WriteLog(module, operation, message, LogLevel.Info, details);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warning(string module, string operation, string message, string details = null)
        {
            WriteLog(module, operation, message, LogLevel.Warning, details);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string module, string operation, string message, Exception ex = null, string details = null)
        {
            string fullDetails = details ?? string.Empty;
            if (ex != null)
            {
                fullDetails += $"\n异常类型: {ex.GetType().Name}\n异常消息: {ex.Message}\n堆栈跟踪:\n{ex.StackTrace}";

                // 包含内部异常信息
                var innerEx = ex.InnerException;
                int depth = 0;
                while (innerEx != null && depth < 5) // 限制深度防止无限递归
                {
                    fullDetails += $"\n内部异常[{depth + 1}]: {innerEx.GetType().Name}: {innerEx.Message}";
                    innerEx = innerEx.InnerException;
                    depth++;
                }
            }

            WriteLog(module, operation, message, LogLevel.Error, fullDetails);
        }

        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        public static void Fatal(string module, string operation, string message, Exception ex = null, string details = null)
        {
            string fullDetails = details ?? string.Empty;
            if (ex != null)
            {
                fullDetails += $"\n致命异常: {ex.GetType().Name}: {ex.Message}\n堆栈跟踪:\n{ex.StackTrace}";
            }

            WriteLog(module, operation, message, LogLevel.Fatal, fullDetails);
        }

        /// <summary>
        /// 向后兼容的方法
        /// </summary>
        [Obsolete("请使用新的方法重载")]
        public static void WriteLog(string operation, string message, bool isError = false, string details = null)
        {
            var level = isError ? LogLevel.Error : LogLevel.Info;
            WriteLog("System", operation, message, level, details);
        }

        /// <summary>
        /// 向后兼容的方法
        /// </summary>
        [Obsolete("请使用Error方法")]
        public static void WriteErrorLog(string operation, string errorDetails, Exception ex = null)
        {
            Error("System", operation, errorDetails, ex);
        }

        /// <summary>
        /// 核心日志写入方法
        /// </summary>
        private static void WriteLog(string module, string operation, string message, LogLevel level, string details = null)
        {
            // 检查日志级别是否满足最低要求
            if (level < _config.MinimumLogLevel)
            {
                return;
            }

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Module = module,
                Operation = operation,
                Message = message,
                Level = level,
                Details = details,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            // 将日志条目加入队列
            _logQueue.Enqueue(logEntry);
            _logEvent.Set();
        }

        /// <summary>
        /// 处理日志队列的后台线程
        /// </summary>
        private static void ProcessLogQueue()
        {
            var batch = new System.Collections.Generic.List<LogEntry>(100);

            while (_isRunning)
            {
                _logEvent.Wait();

                // 批量处理日志条目
                while (_logQueue.TryDequeue(out var logEntry) && batch.Count < 100)
                {
                    batch.Add(logEntry);
                }

                if (batch.Count > 0)
                {
                    try
                    {
                        ProcessLogBatch(batch);
                    }
                    catch (Exception ex)
                    {
                        // 日志处理失败时写入控制台
                        Console.WriteLine($"日志处理失败: {ex.Message}");
                    }
                    finally
                    {
                        batch.Clear();
                    }
                }

                _logEvent.Reset();

                // 短暂休眠避免CPU占用过高
                if (_logQueue.IsEmpty)
                {
                    Thread.Sleep(100);
                }
            }

            // 处理剩余日志
            ProcessRemainingLogs();
        }

        /// <summary>
        /// 处理日志批次
        /// </summary>
        private static void ProcessLogBatch(System.Collections.Generic.List<LogEntry> batch)
        {
            foreach (var logEntry in batch)
            {
                try
                {
                    // 写入控制台
                    if (_config.EnableConsoleLogging)
                    {
                        WriteToConsole(logEntry);
                    }

                    // 写入文件
                    if (_config.EnableFileLogging)
                    {
                        WriteToFile(logEntry);
                    }

                    // 写入数据库
                    if (_config.EnableDatabaseLogging)
                    {
                        WriteToDatabase(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    // 单个日志条目处理失败不应影响其他日志
                    Console.WriteLine($"处理单个日志条目失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 写入控制台
        /// </summary>
        private static void WriteToConsole(LogEntry logEntry)
        {
            var color = logEntry.Level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEntry.Level,-7}] [{logEntry.Module}/{logEntry.Operation}] {logEntry.Message}");

            if (!string.IsNullOrEmpty(logEntry.Details))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  详情: {logEntry.Details}");
            }

            Console.ResetColor();
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        private static void WriteToFile(LogEntry logEntry)
        {
            string logPath = GetLogFilePath(logEntry);
            string logMessage = FormatLogMessage(logEntry);

            try
            {
                // 检查文件大小并处理日志轮转
                ManageLogFileSize(logPath);

                // 写入文件（使用File.AppendAllText简化，实际生产环境可能需要更复杂的处理）
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        private static string GetLogFilePath(LogEntry logEntry)
        {
            string date = logEntry.Timestamp.ToString("yyyy-MM-dd");
            string moduleDir = Path.Combine(_config.LogDirectory, logEntry.Module);

            if (!Directory.Exists(moduleDir))
            {
                Directory.CreateDirectory(moduleDir);
            }

            return Path.Combine(moduleDir, $"{date}.log");
        }

        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private static string FormatLogMessage(LogEntry logEntry)
        {
            return $"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] " +
                   $"[{logEntry.Level,-7}] " +
                   $"[线程:{logEntry.ThreadId:D4}] " +
                   $"[{logEntry.Module}/{logEntry.Operation}] " +
                   $"{logEntry.Message}" +
                   (string.IsNullOrEmpty(logEntry.Details) ? "" : $"\n  详情: {logEntry.Details}");
        }

        /// <summary>
        /// 管理日志文件大小（简单的日志轮转）
        /// </summary>
        private static void ManageLogFileSize(string logPath)
        {
            if (!File.Exists(logPath))
            {
                return;
            }

            var fileInfo = new FileInfo(logPath);
            long maxSize = _config.MaxLogFileSizeMB * 1024L * 1024L;

            if (fileInfo.Length > maxSize)
            {
                PerformLogRotation(logPath);
            }
        }

        /// <summary>
        /// 执行日志轮转
        /// </summary>
        private static void PerformLogRotation(string logPath)
        {
            try
            {
                string directory = Path.GetDirectoryName(logPath);
                string fileName = Path.GetFileNameWithoutExtension(logPath);
                string extension = Path.GetExtension(logPath);

                // 删除最旧的文件
                var logFiles = Directory.GetFiles(directory, $"{fileName}.*{extension}");
                if (logFiles.Length >= _config.MaxLogFileCount)
                {
                    Array.Sort(logFiles);
                    File.Delete(logFiles[0]);
                }

                // 重命名当前文件
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string newPath = Path.Combine(directory, $"{fileName}.{timestamp}{extension}");
                File.Move(logPath, newPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志轮转失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 写入数据库
        /// </summary>
        private static void WriteToDatabase(LogEntry logEntry)
        {
            try
            {
                var logModel = new LogModel
                {
                    UserType = "system",
                    LogType = GetLogType(logEntry.Level),
                    Level = logEntry.Level.ToString(),
                    Message = logEntry.Message,
                    Module = logEntry.Module,
                    Operation = logEntry.Operation,
                    Details = logEntry.Details,
                    UserId = "System",
                    IpAddress = GetLocalIpAddress(),
                    CreateTime = logEntry.Timestamp,
                    IsArchived = "False"
                };

                LogsDbContext.Insert(logModel);
            }
            catch (Exception ex)
            {
                // 数据库写入失败时记录到控制台
                Console.WriteLine($"数据库日志记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据日志级别获取日志类型
        /// </summary>
        private static string GetLogType(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "Debug",
                LogLevel.Info => "Info",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Fatal => "Fatal",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 获取本地IP地址
        /// </summary>
        private static string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // 忽略获取IP地址的异常
            }

            return "127.0.0.1";
        }

        /// <summary>
        /// 确保日志目录存在
        /// </summary>
        private static void EnsureLogDirectory()
        {
            try
            {
                if (!Directory.Exists(_config.LogDirectory))
                {
                    Directory.CreateDirectory(_config.LogDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建日志目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理剩余日志
        /// </summary>
        private static void ProcessRemainingLogs()
        {
            var remainingLogs = new System.Collections.Generic.List<LogEntry>();
            while (_logQueue.TryDequeue(out var logEntry))
            {
                remainingLogs.Add(logEntry);
            }

            if (remainingLogs.Count > 0)
            {
                ProcessLogBatch(remainingLogs);
            }
        }

        /// <summary>
        /// 应用程序退出事件处理
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        /// <summary>
        /// 应用程序域卸载事件处理
        /// </summary>
        private static void OnDomainUnload(object sender, EventArgs e)
        {
            Shutdown();
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            _isRunning = false;
            _logEvent.Set();

            if (_logThread.IsAlive)
            {
                _logThread.Join(5000); // 等待5秒
            }

            Console.WriteLine("日志系统已关闭");
        }

        /// <summary>
        /// 日志条目内部类
        /// </summary>
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Module { get; set; }
            public string Operation { get; set; }
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public string Details { get; set; }
            public int ThreadId { get; set; }
        }
    }
}