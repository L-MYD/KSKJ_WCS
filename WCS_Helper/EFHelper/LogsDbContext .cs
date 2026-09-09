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
        // 避免无限递归，不再调用 Insert 记录错误，而是直接写入文件
        SaveErrToText(message, $"{details}\n异常信息: {ex.Message}");
    }

    public static void SaveErrToText(string detail, string reason)
    {
        const string FilePath = @"C:\LogErrLog";
        var fileName = DateTime.UtcNow.ToString("yyyyMMdd");
        StreamWriter sw = null;
        FileStream fs = null;
        try
        {
            if (!Directory.Exists(FilePath))
                Directory.CreateDirectory(FilePath);
            fileName = "LogErrLog_" + fileName + ".txt";
            var fullPath = Path.Combine(FilePath, fileName);
            if (!File.Exists(fullPath))
            {
                fs = File.Create(fullPath);
                fs.Close();
                fs.Dispose();
            }
            sw = File.AppendText(fullPath);
            var addTxt = $"DealMessage:{detail} \n Reason:{reason} \n Datetime:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} \n";
            sw.Write(addTxt);
            sw.Close();
        }
        catch
        {
            sw?.Close();
            fs?.Close();
        }
    }
    #endregion

    #region 插入
    public static bool Insert(LogModel log)
    {
        try
        {
            // 强制使用 UTC 时间，避免时区问题
            log.CreateTime = DateTime.UtcNow;
            using var context = CreateDbContext();
            context.Logs.Add(log);
            int result = context.SaveChanges();
            return result > 0;
        }
        catch (Exception ex)
        {
            // 获取内部异常详细信息（PostgreSQL 错误通常在 InnerException）
            var innerEx = ex.InnerException;
            var errorMsg = innerEx != null ? innerEx.Message : ex.Message;
            var stackTrace = innerEx != null ? innerEx.StackTrace : ex.StackTrace;

            // 写入文件，避免递归调用 Insert 方法
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {errorMsg}\n{stackTrace}\n";
            System.IO.File.AppendAllText("log_error.txt", logEntry);

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

public static class Logger
{
    // 日志目录可从配置文件读取，默认值备用
    private static readonly string LogDirectory;

    static Logger()
    {
        // 尝试从配置文件读取日志路径，若失败则使用默认路径
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            LogDirectory = configuration["Logging:FileLogDirectory"] ?? @"C:\Logs\TaskLogs";
        }
        catch
        {
            LogDirectory = @"C:\Logs\TaskLogs";
        }

        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    public static void SaveErrToText(string detail, string reason)
    {
        WriteToFile(detail, reason, isAsync: false).GetAwaiter().GetResult(); // 同步调用异步实现（避免重复代码）
    }

    public static Task SaveErrToTextAsync(string detail, string reason)
    {
        return WriteToFile(detail, reason, isAsync: true);
    }

    private static async Task WriteToFile(string detail, string reason, bool isAsync)
    {
        try
        {
            string fileName = $"LogErrLog_{DateTime.UtcNow:yyyyMMdd}.txt";
            string filePath = Path.Combine(LogDirectory, fileName);

            string content = $"DealMessage:{detail}\nReason:{reason}\nDatetime:{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n";

            if (isAsync)
            {
                await File.AppendAllTextAsync(filePath, content);
            }
            else
            {
                File.AppendAllText(filePath, content);
            }
        }
        catch (Exception ex)
        {
            // 日志记录失败时的后备处理：输出到控制台或系统事件（避免无限递归）
            Console.WriteLine($"[Logger Error] {ex.Message}");
        }
    }
}