using System.Text.Json.Serialization;

namespace QMSWebApiCore.Models
{
    public class M_PreLoad
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
        
        [JsonPropertyName("Employee1")]
        public string? Employee1 { get; set; }
        
        [JsonPropertyName("Employee2")]
        public string? Employee2 { get; set; }
        
        [JsonPropertyName("Employee3")]
        public string? Employee3 { get; set; }
        
        [JsonPropertyName("Employee4")]
        public string? Employee4 { get; set; }
        
        [JsonPropertyName("Employee5")]
        public string? Employee5 { get; set; }
        
        [JsonPropertyName("Employee6")]
        public string? Employee6 { get; set; }
        
        [JsonPropertyName("Employee7")]
        public string? Employee7 { get; set; }
        
        [JsonPropertyName("Employee8")]
        public string? Employee8 { get; set; }
        
        [JsonPropertyName("Employee9")]
        public string? Employee9 { get; set; }
        
        [JsonPropertyName("Employee10")]
        public string? Employee10 { get; set; }
        
        [JsonPropertyName("PreCoolStatusID")]
        public string? PreCoolStatusID { get; set; }
        
        [JsonPropertyName("PreCoolStatusText")]
        public string? PreCoolStatusText { get; set; }
        
        [JsonPropertyName("PreLoadStatusID")]
        public string? PreLoadStatusID { get; set; }
        
        [JsonPropertyName("PreLoadStatusText")]
        public string? PreLoadStatusText { get; set; }
        
        [JsonPropertyName("PreLoadRemark")]
        public string? PreLoadRemark { get; set; }
    }
}
