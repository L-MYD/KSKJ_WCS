using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// LED显示
    /// </summary>
    public class LEDModel
    {
        /// <summary>
        /// Wcs流水号
        /// </summary>
        public string WCSId {  get; set; }

        /// <summary>
        /// 托盘号
        /// </summary>
        public string Palno { get; set; }

        /// <summary>
        /// 口地址
        /// </summary>
        public string Port {  get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime Tkdat { get; set; }

        /// <summary>
        /// 状态 Open：打开 Close：熄灭
        /// </summary>
        public string Statu { get; set; }
    }
}
