using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WCS_Helper.Mapper;
using WCS_Models.WCSModel.LogModel;

using WCS_IServices;
namespace WCS_Services
{

    public class LogService : ILogService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
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
                CreateTime = DateTime.UtcNow
            };

            return LogsDbContext.Insert(log);
        }

        // Web应用日志
        public bool AddWebLog(string Message, string Operation, string Details, string UserId, string Level = "INFO", string Module = "Web")
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
                CreateTime = DateTime.UtcNow
            };

            return LogsDbContext.Insert(log);
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
                CreateTime = DateTime.UtcNow
            };

            return LogsDbContext.Insert(log);
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
                CreateTime = DateTime.UtcNow
            };

            return LogsDbContext.Insert(log);
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
                CreateTime = DateTime.UtcNow
            };

            return LogsDbContext.Insert(log);
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
                CreateTime = DateTime.UtcNow
            };

            return LogsDbContext.Insert(log);
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
                    string ip = request.Headers["X-Forwarded-For"].ToString() ??
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

                    return ip;
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