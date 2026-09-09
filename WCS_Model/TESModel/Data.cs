using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.TESModel
{
    public class Data
    {
        public int count { get; set; }
        public int curPageNum { get; set; }
        public List<PodD> podList { get; set; }
    }
}
