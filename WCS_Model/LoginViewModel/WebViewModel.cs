using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WCS_Models.LoginViewModel
{
    public class TaskQueryDto
    {
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 20;
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public string keyword { get; set; }
        public string podId { get; set; }
        public string location { get; set; }
        public string agvId { get; set; }
        public string sortBy { get; set; }
        public string sortOrder { get; set; }
        public DateTime? creationStartTime { get; set; }
        public DateTime? creationEndTime { get; set; }
        public DateTime? completionStartTime { get; set; }
        public DateTime? completionEndTime { get; set; }
        public int? minDuration { get; set; }
        public int? maxDuration { get; set; }
        public string priority { get; set; }
        public string exceptionType { get; set; }
    }

    public class TaskHistoryQueryParams
    {
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 20;
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public string keyword { get; set; }
        public string podId { get; set; }
        public string location { get; set; }
        public string agvId { get; set; }
        public string sortBy { get; set; }
        public string sortOrder { get; set; }
        public DateTime creationStartTime { get; set; }
        public DateTime creationEndTime { get; set; }
        public DateTime completionStartTime { get; set; }
        public DateTime completionEndTime { get; set; }
        public int? minDuration { get; set; }
        public int? maxDuration { get; set; }
        public string priority { get; set; }
        public string exceptionType { get; set; }
    }
}
