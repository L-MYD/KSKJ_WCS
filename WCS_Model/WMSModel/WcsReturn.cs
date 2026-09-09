using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.WMSModel
{
    /// <summary>
    /// WCS返回的参数
    /// </summary>
    public class WcsReturn
    {
        public int returnCode { get; set; }
        public string returnMsg { get; set; }
    }
}
