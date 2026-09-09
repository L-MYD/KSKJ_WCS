using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models
{
    /// <summary>
    /// WCS TO Tes接口调用公共参数
    /// </summary>
    public class ToTesPublicParameters
    {
        /// <summary>
        /// 仓库ID
        /// </summary>
        public string WarehouseID { get; set; } = "HETU";

        /// <summary>
        /// 请求编号，唯一幂等校验
        /// </summary>
        public string RequestID { get; set; }=Math.Abs(Guid.NewGuid().GetHashCode()).ToString("D16");

        /// <summary>
        /// 请求时间(yyyy-MM-dd HH:mm:ss)
        /// </summary>
        public string RequestTime { get; set; }

        /// <summary>
        /// 上位系统标识：WCS, WMS, WES
        /// </summary>
        public string ClientCode { get; set; } = "WCS";

        /// <summary>
        /// 令牌号，暂不启用
        /// </summary>
        public string TokenCode { get; set; }

        /// <summary>
        /// 语言，默认中文
        /// </summary>
        public string Lang { get; set; }

        public string LayoutID { get; set; } = "600874436849041427";
    }


}
