using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace WCS_Models.CTUModel
{
    /// <summary>
    /// 返回海柔的参数
    /// </summary>
    public class ReturnHaiRou
    {
        /// <summary>
        /// 响应状态码。 0 ：正常。 非0：异常。
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// 响应消息补充说明，success 或其他异常消息。
        /// </summary>
        public string msg { get; set; }

        /// <summary>
        /// 返回的数据。
        /// </summary>
        public JsonObject data { get; set; }
    }
}
