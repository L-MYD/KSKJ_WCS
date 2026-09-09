using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class Content
    {
        public int taskID { get; set; }
        public string bizID { get; set; }
        public string clientCode { get; set; }
        public string deviceType { get; set; }
        public string robotID { get; set; }
        public string podID { get; set; }
        public PodInfo podInfo { get; set; }
        public int priority { get; set; }
        public int taskType { get; set; }
        public int status { get; set; }
        public int errorCode { get; set; }
        public string errorReason { get; set; }
        public int desType { get; set; }
        public string desNodeID { get; set; }
        public string desStationCodes { get; set; }
        public string desStorageID { get; set; }
        public string desZoneCode { get; set; }
        public string result { get; set; }
        public string createTime { get; set; }
        public string finishTime { get; set; }
    }
}
