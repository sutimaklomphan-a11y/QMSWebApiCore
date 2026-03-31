using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_FinishPreLoad
    {
        [JsonPropertyName("DCCode")]
        public string DCCode { get; set; }

        [JsonPropertyName("Search")]
        public string? Search { get; set; }

        [JsonPropertyName("PlanBigCType")]
        public string? PlanBigCType { get; set; }

        [JsonPropertyName("PlanDate")]
        public string? PlanDate { get; set; }

        [JsonPropertyName("PlanNo")]
        public string? PlanNo { get; set; }

        [JsonPropertyName("GroupNo")]
        public string? GroupNo { get; set; }

        [JsonPropertyName("LoadNo")]
        public string? LoadNo { get; set; }

        [JsonPropertyName("DockNo")]
        public string? DockNo { get; set; }

        [JsonPropertyName("PreLoadActionDate")]
        public string? PreLoadActionDate { get; set; }

        [JsonPropertyName("PreLoadActionBy")]
        public string? PreLoadActionBy { get; set; }

        [JsonPropertyName("PreLoadStatusID")]
        public string? PreLoadStatusID { get; set; }

        [JsonPropertyName("PreLoadStatusText")]
        public string? PreLoadStatusText { get; set; }

        [JsonPropertyName("PreLoadRemark")]
        public string? PreLoadRemark { get; set; }

        [JsonPropertyName("FinishPreLoadActionDate")]
        public string? FinishPreLoadActionDate { get; set; }

        [JsonPropertyName("FinishPreLoadActionBy")]
        public string? FinishPreLoadActionBy { get; set; }

        [JsonPropertyName("FinishPreLoadStatusID")]
        public string? FinishPreLoadStatusID { get; set; }

        [JsonPropertyName("FinishPreLoadStatusText")]
        public string? FinishPreLoadStatusText { get; set; }

        [JsonPropertyName("FinishPreLoadRemark")]
        public string? FinishPreLoadRemark { get; set; }
        
    }
}
