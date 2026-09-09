using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class ContainerBinding
    {
  
        public string warehouseID { get; set; }
        public string clientCode { get; set; }
        public string requestID { get; set; }

        public string stationCode { get; set; }
        public string layoutID { get; set; }
        public string podID { get; set; }

        public string podFace { get; set; }//容器朝向
       
    }
}
