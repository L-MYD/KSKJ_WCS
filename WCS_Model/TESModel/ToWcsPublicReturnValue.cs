using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models
{
    public class ToWcsPublicReturnValue
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public int ReturnCode { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ReturnMsg { get; set; }

        public ToWcsPublicReturnValue(int errorCode, string message)
        {
            ReturnCode = errorCode;
            ReturnMsg = message ?? throw new ArgumentNullException(nameof(message));
        }

        public static ToWcsPublicReturnValue Success(string message = "succ")
        {
            return new ToWcsPublicReturnValue(0, message);
        }

        public static ToWcsPublicReturnValue Error(string message = "error")
        {
            return new ToWcsPublicReturnValue(1, message);
        }
    }

    public class ToWcsPublicReturnValueL : ToWcsPublicReturnValue
    {
        public ToWcsPublicReturnValueL(int errorCode, string message, string usermessage, object data) : base(errorCode, message)
        {
            ReturnCode = errorCode;
            ReturnMsg = message ?? throw new ArgumentNullException(nameof(message));
            ReturnUserMsgg = usermessage ?? throw new ArgumentNullException(nameof(usermessage));
            Data = data;
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public object ReturnUserMsgg { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public object Data { get; set; }

        public static ToWcsPublicReturnValueL Success(string message = "succ",string usermessage="成功",object data=null)
        {
            return new ToWcsPublicReturnValueL(0, message,usermessage,data );
        }

        public static ToWcsPublicReturnValueL Error(string message = "error", string usermessage = "失败", object data = null)
        {
            return new ToWcsPublicReturnValueL(1, message, usermessage, data);
        }
    }
}
