using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using WCS_Common.Enums;
using WCS_Common.Helper;
using WCS_Helper.Database;
using WCS_Models.WCSModel.LogModel;

using WCS_IServices;
namespace WCS_Services
{
    /// <summary>
    /// nan_T 2026-09-09：日志服务实现（参考 DTcms.Core.Services 的分层写法）。
    /// 继承 <see cref="BaseService"/>，通过 <see cref="IDbContextFactory"/> 创建写上下文落库 logs 表，
    /// 不再依赖静态 LogsDbContext；所有日志只写数据库与控制台，不产生任何 txt 文件。
    /// 日志来源区分：PLC（PLC日志）、API（接口日志）、Web（操作日志）、系统/TES/RCS 等。
    /// </summary>
    public class LogService : BaseService, ILogService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 构造函数：注入数据库工厂与 HTTP 上下文访问器
        /// </summary>
        public LogService(IDbContextFactory contextFactory, IHttpContextAccessor httpContextAccessor)
            : base(contextFactory)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// nan_T 2026-09-09：日志统一落库入口（写库上下文，失败返回 false 且不抛异常，避免日志拖垮主流程）。
        /// </summary>
        private bool InsertLog(LogModel log)
        {
            try
            {
                log.CreateTime = DateTimeHelper.CnNow;
                if (string.IsNullOrEmpty(log.IsArchived))
                {
                    log.IsArchived = "False";
                }

                using var context = _contextFactory.CreateContext(WriteRoRead.Write);
                context.Logs.Add(log);
                return context.SaveChanges() > 0;
            }
            catch (Exception ex)
            {
                // nan_T 2026-09-09：日志落库失败仅输出控制台，绝不写 txt 文件，也不向上抛异常
                Console.WriteLine($"[LogService] 日志写库失败: {ex.Message}");
                return false;
            }
        }

        // System系统日志
        public bool AddSystemLog(string Message, string Operation, string Details, string Level = "INFO", string Module = "System", string UserId = "system")
        {
            var log = new LogModel
            {
                UserType = "system",
                LogType = "系统日志",
                Level = Level,
                Message = Message,
                Module = Module,
                Operation = Operation,
                Details = Details,
                UserId = UserId,
                IpAddress = GetLocalIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        // Web应用日志
        // nan_T 2026-09-09：参数顺序修正为 (…, Level, Module, UserId)，与 DataMonitoringService 调用点对齐
        public bool AddWebLog(string Message, string Operation, string Details, string Level = "INFO", string Module = "Web", string UserId = "system")
        {
            var log = new LogModel
            {
                UserType = "web",
                LogType = "操作日志",
                Level = Level,
                Message = Message,
                Module = Module,
                Operation = Operation,
                Details = Details,
                UserId = UserId,
                IpAddress = GetClientIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        /// <summary>
        /// nan_T 2026-09-09：PLC 通讯日志（与 API 接口日志区分：usertype=plc / logtype=PLC日志 / module=PLC）
        /// </summary>
        public bool AddPlcLog(string Message, string Operation, string Details, string Level = "INFO")
        {
            var log = new LogModel
            {
                UserType = "plc",
                LogType = "PLC日志",
                Level = Level,
                Message = Message,
                Module = "PLC",
                Operation = Operation,
                Details = Details,
                UserId = "plc",
                IpAddress = GetLocalIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        // API接口日志
        public bool AddApiLog(string Message, string Operation, string Details, string UserId = "api", string Level = "INFO", string Module = "API")
        {
            var log = new LogModel
            {
                UserType = "api",
                LogType = "接口日志",
                Level = Level,
                Message = Message,
                Module = Module,
                Operation = Operation,
                Details = Details,
                UserId = UserId,
                IpAddress = GetClientIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        // TES系统日志
        public bool AddTesLog(string Message, string Operation, string Details, string UserId = "tes", string Level = "INFO", string Module = "TES")
        {
            var log = new LogModel
            {
                UserType = "TES",
                LogType = "TES日志",
                Level = Level,
                Message = Message,
                Module = Module,
                Operation = Operation,
                Details = Details,
                UserId = UserId,
                IpAddress = GetLocalIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        // RCS系统日志
        public bool AddRcsLog(string Message, string Operation, string Details, string UserId = "rcs", string Level = "INFO", string Module = "RCS")
        {
            var log = new LogModel
            {
                UserType = "RCS",
                LogType = "RCS日志",
                Level = Level,
                Message = Message,
                Module = Module,
                Operation = Operation,
                Details = Details,
                UserId = UserId,
                IpAddress = GetLocalIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        // 通用方法 - 如果需要更灵活的日志记录
        public bool AddCustomLog(string userType, string logType, string Level, string Message, string Module, string Operation, string Details, string UserId, string ipAddress = null)
        {
            var log = new LogModel
            {
                UserType = userType,
                LogType = logType,
                Level = Level,
                Message = Message,
                Module = Module,
                Operation = Operation,
                Details = Details,
                UserId = UserId,
                IpAddress = ipAddress ?? GetLocalIpAddress(),
                CreateTime = DateTimeHelper.CnNow
            };

            return InsertLog(log);
        }

        // 获取本地IP地址
        private string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // 获取客户端IP地址（用于Web和API）
        private string GetClientIpAddress()
        {
            try
            {
                // 如果在Web环境中
                if (_httpContextAccessor?.HttpContext != null)
                {
                    var request = _httpContextAccessor.HttpContext.Request;

                    // 优先从代理转发头中获取真实IP
                    // nan_T 2026-09-09：RemoteIpAddress?.ToString() 可能为 null，ip 声明为可空
                    string? ip = request.Headers["X-Forwarded-For"].ToString() ??
                               request.Headers["X-Real-IP"].ToString() ??
                               _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

                    // 如果从代理头中获取到多个IP（如代理链），取第一个
                    if (!string.IsNullOrEmpty(ip) && ip.Contains(","))
                    {
                        ip = ip.Split(',')[0].Trim();
                    }

                    // 如果获取到的IP无效，则使用RemoteIpAddress
                    if (string.IsNullOrEmpty(ip) || ip.ToLower() == "unknown" || ip == "::1")
                        ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

                    // 处理本地地址
                    if (ip == "::1" || ip == "127.0.0.1")
                        ip = "127.0.0.1";

                    return ip ?? "127.0.0.1";
                }
            }
            catch
            {
                // 忽略异常
            }

            return GetLocalIpAddress();
        }
    }
}
