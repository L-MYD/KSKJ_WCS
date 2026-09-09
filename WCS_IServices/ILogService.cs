namespace WCS_IServices
{
    /// <summary>
    /// 日志服务接口：统一系统、Web、接口、TES、RCS 等各模块的日志写入能力。
    /// 由 WCS_Services 层的 LogService 实现。
    /// </summary>
    public interface ILogService
    {
        /// <summary>写入系统日志</summary>
        bool AddSystemLog(string Message, string Operation, string Details, string Level = "INFO", string Module = "System", string UserId = "system");

        /// <summary>写入 Web 端操作日志</summary>
        bool AddWebLog(string Message, string Operation, string Details, string UserId, string Level = "INFO", string Module = "Web");

        /// <summary>写入对外 API 接口日志</summary>
        bool AddApiLog(string Message, string Operation, string Details, string UserId = "api", string Level = "INFO", string Module = "API");

        /// <summary>写入 TES 系统日志</summary>
        bool AddTesLog(string Message, string Operation, string Details, string UserId = "tes", string Level = "INFO", string Module = "TES");

        /// <summary>写入 RCS 系统日志</summary>
        bool AddRcsLog(string Message, string Operation, string Details, string UserId = "rcs", string Level = "INFO", string Module = "RCS");

        /// <summary>写入自定义类型日志（最通用的重载）</summary>
        bool AddCustomLog(string userType, string logType, string Level, string Message, string Module, string Operation, string Details, string UserId, string ipAddress = null);
    }
}
