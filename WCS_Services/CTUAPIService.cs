using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WCS_Helper.Mapper;
using WCS_Models.CTUModel;
using WCS_Models.CTUModel.WCS_Models.CTUModel;
using WCS_Models.TESModel;
using WCS_Models.WCSModel.LogModel;
using WCS_Models.WMSModel;

namespace WCS_Services
{
    public class CTUAPIService
    {
         string ctuapiip = "http://10.10.200.90:8003/";
        /// <summary>
        /// 查询TES任务详情
        /// </summary>
        /// <param name="TesTskID">TES任务ID</param>
        //public CTUCreateResponse CTUCreateTask(WMSTaskDisModel wmstaskmodel)
        //{
        //    try
        //    {
        //        HttpClient client = new HttpClient();
        //        string serviceAddress = ctuapiip + "/task/create";

        //        CTUCreateModel taskmodel = new CTUCreateModel();
        //        taskmodel.taskType = "putaway";
        //        taskmodel.groupPriority=wmstaskmodel.Num != null ? int.Parse(wmstaskmodel.Num) : 0;
        //        CTUCreatetaskModel task = new CTUCreatetaskModel();
        //        task.taskCode=wmstaskmodel.WMSId;
        //        task.taskPriority=wmstaskmodel.Num != null ? int.Parse(wmstaskmodel.Num) : 0;
        //        CTUtaskDescribe cTUtaskDescribe = new CTUtaskDescribe();
        //        cTUtaskDescribe



        //        // 记录HTTP请求
        //        var requestModel = new getTaskDetail { taskID = TesTskID };
        //        string requestJson = JsonConvert.SerializeObject(requestModel);
        //        var httpRequestLog = new LogModel
        //        {
        //            UserType = "http",
        //            LogType = "HTTP Request",
        //            Level = "Info",
        //            Message = "调用TES任务详情API",
        //            Module = "ToTesApiService",
        //            Operation = "getTaskDetail",
        //            Details = $"请求路径: {serviceAddress}\n完整请求参数JSON:\n{requestJson}",
        //            UserId = "System",
        //            IpAddress = "System",
        //            CreateTime = DateTime.UtcNow,
        //            IsArchived = "False"
        //        };
        //        LogsDbContext.Insert(httpRequestLog);

        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
        //        request.Method = "POST";
        //        request.ContentType = "application/json";
        //        getTaskDetail taskmodel = new getTaskDetail();
        //        taskmodel.taskID = TesTskID;
        //        string strJson = JsonConvert.SerializeObject(taskmodel);

        //        using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
        //        {
        //            dataStream.Write(strJson);
        //            dataStream.Close();
        //        }

        //        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //        string encoding = response.ContentEncoding;
        //        if (encoding == null || encoding.Length < 1)
        //        {
        //            encoding = "UTF-8"; //默认编码  
        //        }

        //        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
        //        string retString = reader.ReadToEnd();

        //        // 记录HTTP响应
        //        var httpResponseLog = new LogModel
        //        {
        //            UserType = "http",
        //            LogType = "HTTP Response",
        //            Level = "Info",
        //            Message = "TES任务详情API响应",
        //            Module = "ToTesApiService",
        //            Operation = "getTaskDetail",
        //            Details = $"响应状态码: {(int)response.StatusCode}\n完整响应内容JSON:\n{retString}",
        //            UserId = "System",
        //            IpAddress = "System",
        //            CreateTime = DateTime.UtcNow,
        //            IsArchived = "False"
        //        };
        //        LogsDbContext.Insert(httpResponseLog);

        //        TaskInfo getTask = JsonConvert.DeserializeObject<TaskInfo>(retString);
        //        return getTask;
        //    }
        //    catch (Exception ex)
        //    {
        //        // 记录异常信息
        //        var errorLog = new LogModel
        //        {
        //            UserType = "http",
        //            LogType = "HTTP Error",
        //            Level = "Error",
        //            Message = "TES任务详情API调用异常",
        //            Module = "ToTesApiService",
        //            Operation = "getTaskDetail",
        //            Details = $"异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}",
        //            UserId = "System",
        //            IpAddress = "System",
        //            CreateTime = DateTime.UtcNow,
        //            IsArchived = "False"
        //        };
        //        LogsDbContext.Insert(errorLog);
        //        throw;
        //    }
        //}
    }
}
