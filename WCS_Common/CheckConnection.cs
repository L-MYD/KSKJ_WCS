using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Common
{
    public class CheckConnection
    {
        // 检查指定机器名的连接状态
        public static bool Ping(string machineName)
        {
            bool available = false;// 默认连接不可用

            // 若机器名为空或null，则直接返回连接不可用
            if (string.IsNullOrEmpty(machineName))
            {
                return available;
            }

            Ping ping = new Ping();// 创建Ping对象

            PingReply reply = ping.Send(machineName, 5000);// 发送Ping请求并获取响应

            // 若响应状态为成功，则连接可用
            if (reply.Status == IPStatus.Success)
            {
                available = true;
            }

            return available;
        }
    }
}
