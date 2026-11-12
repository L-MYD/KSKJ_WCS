using System;

namespace WCS_Common.Helper
{
    /// <summary>
    /// nan_T 2026-11-12：统一时间取值入口。
    /// 原代码大量使用 DateTime.UtcNow 写入日志/任务时间，部署在中国（UTC+8）环境时
    /// 写入值比本地时间慢 8 小时，影响日志查看与任务时间判断。
    /// 本类统一返回中国标准时间（UTC+8），与服务器操作系统时区无关；
    /// 后续所有时间取值应使用 DateTimeHelper.CnNow 替代 DateTime.UtcNow。
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// 中国标准时间（UTC+8）时区对象。
        /// Windows: "China Standard Time"；Linux/Mac: "Asia/Shanghai"。
        /// </summary>
        private static readonly TimeZoneInfo ChinaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows()
                ? "China Standard Time"
                : "Asia/Shanghai");

        /// <summary>
        /// 获取当前中国标准时间（UTC+8）。
        /// </summary>
        public static DateTime CnNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaTimeZone);
    }
}
