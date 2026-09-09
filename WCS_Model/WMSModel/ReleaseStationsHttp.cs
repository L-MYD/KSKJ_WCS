using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace WCS_Models.WMSModel
{

    /// <summary>
    /// 释放站HTTP请求/响应模型
    /// </summary>
    public class ReleaseStationsHttp
    {
        /// <summary>
        /// WCS标识
        /// </summary>
        [JsonPropertyName("WCsId")]
        public string WcsId { get; set; }

        /// <summary>
        /// 出库端口
        /// </summary>
        [JsonPropertyName("OutPort")]
        public string OutPort { get; set; }
    }

    /// <summary>
    /// 释放站HTTP响应模型
    /// </summary>
    public record ReleaseStationsHttpResqon
    {
        /// <summary>
        /// MSA类型
        /// </summary>
        [JsonPropertyName("msaType")]
        public string MsaType { get; init; } = string.Empty;

        /// <summary>
        /// MES文本消息
        /// </summary>
        [JsonPropertyName("mesText")]
        public string MesText { get; init; } = string.Empty;

        /// <summary>
        /// WCS标识
        /// </summary>
        [JsonPropertyName("WcsId")]
        public string WcsId { get; init; } = string.Empty;
    }
}
