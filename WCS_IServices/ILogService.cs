namespace WCS_IServices
{
    /// <summary>
    /// 日志服务接口：统一 PLC、API(接口)、Web、系统、TES、RCS 等各模块日志写入数据库 logs 表的能力。
    /// 由 WCS_Services 层的 LogService 实现。
    /// nan_T 2026-09-09：按 DTcms.Core 分层约定继承 <see cref="IBaseService"/>；
    /// 新增 PLC 分类日志，并修正 AddWebLog 参数顺序（与各调用点实际传参对齐）。
    /// </summary>
    public interface ILogService : IBaseService
    {
        /// <summary>
        /// 写入系统日志
        /// </summary>
        /// <param name="Message">日志消息</param>
        /// <param name="Operation">操作/环节</param>
        /// <param name="Details">详情</param>
        /// <param name="Level">日志级别（INFO/WARN/ERROR）</param>
        /// <param name="Module">模块名</param>
        /// <param name="UserId">操作用户标识</param>
        bool AddSystemLog(string Message, string Operation, string Details, string Level = "INFO", string Module = "System", string UserId = "system");

        /// <summary>
        /// 写入 Web 端操作日志。
        /// nan_T 2026-09-09：参数顺序修正为 (…, Level, Module, UserId)，
        /// 与 DataMonitoringService 等调用点的实际传参一致（原接口 UserId/Level/Module 顺序错位，导致级别和模块写错列）。
        /// </summary>
        /// <param name="Message">日志消息</param>
        /// <param name="Operation">操作/环节</param>
        /// <param name="Details">详情</param>
        /// <param name="Level">日志级别（INFO/WARN/ERROR）</param>
        /// <param name="Module">模块名（如 Task）</param>
        /// <param name="UserId">操作用户标识</param>
        bool AddWebLog(string Message, string Operation, string Details, string Level = "INFO", string Module = "Web", string UserId = "system");

        /// <summary>
        /// nan_T 2026-09-09：写入 PLC 通讯日志（usertype=plc / logtype=PLC日志 / module=PLC），
        /// 与 API 接口日志区分开，便于在 logs 表按来源筛选。
        /// </summary>
        /// <param name="Message">日志消息</param>
        /// <param name="Operation">操作/环节（如 Recv—DB5、Send、自动重发）</param>
        /// <param name="Details">详情（错误原因、报文等）</param>
        /// <param name="Level">日志级别（INFO/WARN/ERROR）</param>
        bool AddPlcLog(string Message, string Operation, string Details, string Level = "INFO");

        /// <summary>
        /// 写入对外 API 接口日志（usertype=api / logtype=接口日志），含 TES/WMS 交互。
        /// </summary>
        bool AddApiLog(string Message, string Operation, string Details, string UserId = "api", string Level = "INFO", string Module = "API");

        /// <summary>写入 TES 系统日志</summary>
        bool AddTesLog(string Message, string Operation, string Details, string UserId = "tes", string Level = "INFO", string Module = "TES");

        /// <summary>写入 RCS 系统日志</summary>
        bool AddRcsLog(string Message, string Operation, string Details, string UserId = "rcs", string Level = "INFO", string Module = "RCS");

        /// <summary>写入自定义类型日志（最通用的重载）</summary>
        bool AddCustomLog(string userType, string logType, string Level, string Message, string Module, string Operation, string Details, string UserId, string ipAddress = null);
    }
}
