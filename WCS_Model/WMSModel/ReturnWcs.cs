using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WMS返回WCS的参数
    /// </summary>
    public class ReturnWcs
    {
        /// <summary>
        /// 有错误是返回错误消息，无错误是返回null
        /// </summary>
        public string rtnMesg { get; set; }

        /// <summary>
        /// 请求处理成功返回"0000000" 其他均为失败
        /// </summary>
        public string rtnCode { get; set; }
    }
}
