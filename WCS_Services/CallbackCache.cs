using System.Collections.Concurrent;
using WCS_Models.WMSModel;

namespace WCS_Services
{
    /// <summary>
    /// 回调缓存管理：用于暂存TES状态回调，待HTTP响应返回后再补发
    /// 解决WMS先收到状态回调后收到创建成功响应导致的时序问题
    /// </summary>
    public static class CallbackCache
    {
        // 任务ID（WMS任务ID） → 所属批次ID 的映射
        private static readonly ConcurrentDictionary<string, string> _taskToBatch = new ConcurrentDictionary<string, string>();

        // 批次ID → 该批次待补发的回调队列
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<CallbackItem>> _batchCallbacks = new ConcurrentDictionary<string, ConcurrentQueue<CallbackItem>>();

        /// <summary>
        /// 注册一个批次，建立任务ID与批次ID的关联，并初始化该批次的回调队列
        /// </summary>
        /// <param name="batchId">批次唯一标识（Guid字符串）</param>
        /// <param name="taskIds">该批次所有WMS任务ID列表</param>
        public static void RegisterBatch(string batchId, IEnumerable<string> taskIds)
        {
            foreach (var taskId in taskIds)
            {
                _taskToBatch[taskId] = batchId;
            }
            _batchCallbacks[batchId] = new ConcurrentQueue<CallbackItem>();
        }

        /// <summary>
        /// 判断某个任务是否属于尚未返回HTTP响应的批次（需要缓存回调）
        /// </summary>
        public static bool IsPending(string taskId)
        {
            return _taskToBatch.ContainsKey(taskId);
        }

        /// <summary>
        /// 缓存一个回调事件（用于后续补发）
        /// </summary>
        /// <param name="taskId">WMS任务ID</param>
        /// <param name="request">待发送给WMS的请求对象</param>
        public static void Enqueue(string taskId, UpdateTaskRequest request)
        {
            if (_taskToBatch.TryGetValue(taskId, out var batchId))
            {
                var queue = _batchCallbacks.GetOrAdd(batchId, new ConcurrentQueue<CallbackItem>());
                queue.Enqueue(new CallbackItem { TaskId = taskId, Request = request });
            }
            else
            {
                // 理论上不会发生，若发生则记录日志（此处可添加日志）
            }
        }

        /// <summary>
        /// 获取指定批次的所有缓存回调，并从缓存中移除该批次
        /// </summary>
        /// <param name="batchId">批次ID</param>
        /// <returns>该批次的所有回调项列表</returns>
        public static List<CallbackItem> DequeueBatch(string batchId)
        {
            if (_batchCallbacks.TryRemove(batchId, out var queue))
            {
                var list = new List<CallbackItem>();
                while (queue.TryDequeue(out var item))
                {
                    list.Add(item);
                }
                // 移除任务映射，释放内存
                foreach (var item in list)
                {
                    _taskToBatch.TryRemove(item.TaskId, out _);
                }
                return list;
            }
            return new List<CallbackItem>();
        }

        /// <summary>
        /// 强制清理批次（异常情况下使用），防止内存泄漏
        /// </summary>
        public static void ClearBatch(string batchId)
        {
            if (_batchCallbacks.TryRemove(batchId, out var queue))
            {
                // 丢弃所有回调（不补发）
                while (queue.TryDequeue(out _)) { }
            }
            // 移除任务映射
            var keys = _taskToBatch.Where(kvp => kvp.Value == batchId).Select(kvp => kvp.Key).ToList();
            foreach (var key in keys)
            {
                _taskToBatch.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 回调项实体，包含任务ID和待发送的请求对象
    /// </summary>
    public class CallbackItem
    {
        public string TaskId { get; set; }
        public UpdateTaskRequest Request { get; set; }
    }
}