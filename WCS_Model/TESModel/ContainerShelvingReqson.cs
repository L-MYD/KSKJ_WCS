using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    using Newtonsoft.Json.Linq;

    public class ContainerShelvingReqson
    {
        public int returnCode { get; set; }
        public string returnMsg { get; set; }
        public string returnUserMsg { get; set; }
        public JObject data { get; set; }   // 改为 JObject
    }
}
