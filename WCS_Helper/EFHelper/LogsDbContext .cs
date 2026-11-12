using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using WCS_Helper;
using WCS_Models.WCSModel.LogModel;
using WCS_Common.Helper;

public static class LogsDbContext
{
    private static readonly string ConnectionString = ConfigurationHelper.GetConnectionString();

    #region 辅助方法
    private static WcsDbContext CreateDbContext()
    {
        return new WcsDbContext();
    }

    private static void LogError(string message, string details, Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string operation = "")
    {
        // nan_T 2026-09-09：不再写 txt 文件，日志落库失败时仅输出控制台，避免无限递归
        SaveErrToText(message, $"{details}\n异常信息: {ex.Message}");
    }

    /// <summary>
    /// nan_T 2026-09-09：原“写错误 txt 文件”方法，现改为将错误日志写入数据库 logs 表（ERROR 级系统日志）；
    /// 方法名保留不变，避免改动大量既有调用点。数据库也不可用时仅输出控制台，绝不落盘 txt。
    /// </summary>
    public static void SaveErrToText(string detail, string reason)
    {
        try
        {
            var log = new LogModel
            {
                UserType = "system",
                LogType = "Error",
                Level = "Error",
                Message = detail,
                Module = "System",
                Operation = "SaveErrToText",
                Details = reason,
                UserId = "System",
                IpAddress = "127.0.0.1",
                CreateTime = DateTimeHelper.CnNow,
                IsArchived = "False"
            };

            // 直接走 EF 落库；Insert 内部已捕获异常并只输出控制台，不会递归回本方法
            using var context = CreateDbContext();
            context.Logs.Add(log);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            // nan_T 2026-09-09：最后兜底：数据库也写不进去时只打印控制台，不再生成任何 txt 文件
            Console.WriteLine($"[SaveErrToText] {detail} | {reason} | DB写入失败: {ex.Message}");
        }
    }
    #endregion

    #region 插入
    public static bool Insert(LogModel log)
    {
        try
        {
            // 强制使用 UTC 时间，避免时区问题
            log.CreateTime = DateTimeHelper.CnNow;
            using var context = CreateDbContext();
            context.Logs.Add(log);
            int result = context.SaveChanges();
            return result > 0;
        }
        catch (Exception ex)
        {
            // nan_T 2026-09-09：日志落库失败不再写 log_error.txt 文件，仅输出控制台，避免产生散落 txt
            var innerEx = ex.InnerException;
            var errorMsg = innerEx != null ? innerEx.Message : ex.Message;
            Console.WriteLine($"[LogsDbContext.Insert] 日志写库失败: {errorMsg}");

            return false;
        }
    }

    public static bool InsertBatch(List<LogModel> logs)
    {
        try
        {
            using var context = CreateDbContext();
            context.Logs.AddRange(logs);
            int result = context.SaveChanges();
            return result == logs.Count;
        }
        catch (Exception ex)
        {
            SaveErrToText("批量插入日志失败: ", ex.Message);
            return false;
        }
    }
    #endregion

    #region 查询
    public static LogModel GetById(int id)
    {
        try
        {
            using var context = CreateDbContext();
            return context.Logs.FirstOrDefault(l => l.Id == id);
        }
        catch (Exception ex)
        {
            SaveErrToText("查询日志失败: ", ex.Message);
            return null;
        }
    }

    public static List<LogModel> GetAll()
    {
        try
        {
            using var context = CreateDbContext();
            return context.Logs.OrderByDescending(l => l.CreateTime).ToList();
        }
        catch (Exception ex)
        {
            SaveErrToText("查询所有日志失败: ", ex.Message);
            return new List<LogModel>();
        }
    }

    public static List<LogModel> GetByCondition(string level = null, string module = null, string userId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            using var context = CreateDbContext();
            var query = context.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
                query = query.Where(l => l.Level == level);
            if (!string.IsNullOrEmpty(module))
                query = query.Where(l => l.Module == module);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);
            if (startDate.HasValue)
                query = query.Where(l => l.CreateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(l => l.CreateTime <= endDate.Value);

            return query.OrderByDescending(l => l.CreateTime).ToList();
        }
        catch (Exception ex)
        {
            SaveErrToText("条件查询日志失败: ", ex.Message);
            return new List<LogModel>();
        }
    }

    public static (List<LogModel> Data, int TotalCount) GetByPage(int pageIndex, int pageSize, string level = null, string module = null)
    {
        try
        {
            using var context = CreateDbContext();
            var query = context.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
                query = query.Where(l => l.Level == level);
            if (!string.IsNullOrEmpty(module))
                query = query.Where(l => l.Module == module);

            int totalCount = query.Count();
            var data = query
                .OrderByDescending(l => l.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (data, totalCount);
        }
        catch (Exception ex)
        {
            SaveErrToText("分页查询日志失败: ", ex.Message);
            return (new List<LogModel>(), 0);
        }
    }
    #endregion

    #region 更新
    public static bool Update(LogModel log)
    {
        try
        {
            using var context = CreateDbContext();
            var existing = context.Logs.FirstOrDefault(l => l.Id == log.Id);
            if (existing == null) return false;

            // 更新所有属性（可根据需要只更新部分）
            context.Entry(existing).CurrentValues.SetValues(log);
            int result = context.SaveChanges();
            return result > 0;
        }
        catch (Exception ex)
        {
            SaveErrToText("更新日志失败: ", ex.Message);
            return false;
        }
    }
    #endregion

    #region 删除
    public static bool Delete(int id)
    {
        try
        {
            using var context = CreateDbContext();
            var entity = context.Logs.FirstOrDefault(l => l.Id == id);
            if (entity == null) return false;
            context.Logs.Remove(entity);
            int result = context.SaveChanges();
            return result > 0;
        }
        catch (Exception ex)
        {
            SaveErrToText("删除日志失败: ", ex.Message);
            return false;
        }
    }

    public static bool DeleteBatch(List<int> ids)
    {
        try
        {
            using var context = CreateDbContext();
            var entities = context.Logs.Where(l => ids.Contains(l.Id)).ToList();
            if (!entities.Any()) return false;
            context.Logs.RemoveRange(entities);
            int result = context.SaveChanges();
            return result > 0;
        }
        catch (Exception ex)
        {
            SaveErrToText("批量删除日志失败: ", ex.Message);
            return false;
        }
    }
    #endregion

    #region 归档与统计
    public static bool ArchiveLogs(DateTime beforeDate)
    {
        try
        {
            using var context = CreateDbContext();
            var logsToArchive = context.Logs
                .Where(l => l.CreateTime <= beforeDate && l.IsArchived == "False")
                .ToList();
            foreach (var log in logsToArchive)
            {
                log.IsArchived = "True";
            }
            int result = context.SaveChanges();
            return result > 0;
        }
        catch (Exception ex)
        {
            SaveErrToText("归档日志失败: ", ex.Message);
            return false;
        }
    }

    public static dynamic GetLogStatistics()
    {
        try
        {
            using var context = CreateDbContext();
            var stats = context.Logs
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .ToList();
            return stats;
        }
        catch (Exception ex)
        {
            SaveErrToText("获取日志统计失败: ", ex.Message);
            return null;
        }
    }
    #endregion
}

/// <summary>
/// nan_T 2026-09-09：原“写 txt 错误日志”的静态 Logger，现改为写数据库 logs 表 + 控制台兜底，
/// 不再生成任何 txt 文件。方法签名保持不变，既有调用点无需修改。
/// </summary>
public static class Logger
{
    public static void SaveErrToText(string detail, string reason)
    {
        WriteToDatabase(detail, reason);
    }

    public static Task SaveErrToTextAsync(string detail, string reason)
    {
        WriteToDatabase(detail, reason);
        return Task.CompletedTask;
    }

    /// <summary>
    /// nan_T 2026-09-09：错误日志落库（系统级 ERROR）；数据库不可用时仅输出控制台。
    /// </summary>
    private static void WriteToDatabase(string detail, string reason)
    {
        try
        {
            var log = new LogModel
            {
                UserType = "system",
                LogType = "Error",
                Level = "Error",
                Message = detail,
                Module = "System",
                Operation = "Logger.SaveErrToText",
                Details = reason,
                UserId = "System",
                IpAddress = "127.0.0.1",
                CreateTime = DateTimeHelper.CnNow,
                IsArchived = "False"
            };
            LogsDbContext.Insert(log);
        }
        catch (Exception ex)
        {
            // 最后兜底：只打控制台，不写任何文件
            Console.WriteLine($"[Logger Error] {detail} | {reason} | DB写入失败: {ex.Message}");
        }
    }
}