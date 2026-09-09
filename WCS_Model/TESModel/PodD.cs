using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class PodD
    {
        public string regionCode { get; set; }
        public string podID { get; set; }
        public string barCode { get; set; }
        public int podType { get; set; }
        public int mapID { get; set; }
        public int positionType { get; set; }
        public string storageID { get; set; }
        public string robotID { get; set; }
        public int basketIndex { get; set; }
        public string updateTime { get; set; }
        public string inTime { get; set; }
        public string layoutID { get; set; }
        public string layoutName { get; set; }
        public int length { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string curNodeCode { get; set; }
        public string nodeCode { get; set; }
        public string curPosition { get; set; }
        public string deviceID { get; set; }
        public string elevator { get; set; }
        public int floor { get; set; }
        public int subMapID { get; set; }
        public float currentDir { get; set; }
        public string podExt { get; set; }
    }
}
