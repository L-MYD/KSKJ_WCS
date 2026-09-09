using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class ContainerInfoModel
    {

        public string warehouseID { get; set; }
        public string regionCode { get; set; }
        public string clientCode { get; set; }
        public string podIDs { get; set; }//指定容器号查询
        public string nodeCodes { get; set; }//指定位置编号查询
        public int pageSize { get; set; } = 1000;
        public int pageNum { get; set; } = 1;
    }
}
