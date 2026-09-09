using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class RootMessage
    {
        public long messageID { get; set; }
        public int messageType { get; set; }
        public string warehouseID { get; set; }
        public string createTime { get; set; }
        public Content content { get; set; }
    }
}
