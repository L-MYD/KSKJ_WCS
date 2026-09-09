using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.PLCModel
{
    public class ConveMessageEventArgs : EventArgs
    {
        public readonly string Message;// 消息内容

        // 构造函数，用于初始化消息内容
        public ConveMessageEventArgs(string message)
        {
            Message = message;
        }
    }

    public class ConveMessageEvent
    {
        public delegate void MessageEventHandler(object sender, ConveMessageEventArgs e);// 消息事件处理委托

        public static event MessageEventHandler Recv;// 接收消息事件
        public static event MessageEventHandler Send;// 发送消息事件

        // 触发接收消息事件的方法
        public void OnShowRecv(ConveMessageEventArgs e)
        {
            Recv?.Invoke(this, e);
        }

        // 触发发送消息事件的方法
        public void OnShowSend(ConveMessageEventArgs e)
        {
            Send?.Invoke(this, e);
        }
    }
}
