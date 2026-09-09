using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class TaskReqsonModel
    {

    
        public int returnCode { get; set; }
        public string returnMsg { get; set; }
        public string returnUserMsg { get; set; }
        public DataModel data { get; set; }
    }
}
