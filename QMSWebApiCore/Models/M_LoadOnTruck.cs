using System.Text.Json.Serialization;

namespace QMSWebApiCore.Models
{
    public class M_LoadOnTruck
    {
        [JsonPropertyName("DCCode")]
        public string DCCode { get; set; }
        
        [JsonPropertyName("PlanNo")]
        public string? PlanNo { get; set; }
        
        [JsonPropertyName("PlanDate")]
        public string? PlanDate { get; set; }
        
        [JsonPropertyName("PlanGroupNo")]
        public string? PlanGroupNo { get; set; }
        
        [JsonPropertyName("PlanBigCType")]
        public string? PlanBigCType { get; set; }
        
        [JsonPropertyName("Search")]
        public string? Search { get; set; }
        
        [JsonPropertyName("LoadNo")]
        public string? LoadNo { get; set; }
        
        [JsonPropertyName("ActionBy")]
        public string? ActionBy { get; set; }
        
        [JsonPropertyName("ActionDate")]
        public string? ActionDate { get; set; }
        
        [JsonPropertyName("DockNo")]
        public string? DockNo { get; set; }
        
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
        
        [JsonPropertyName("Remark")]
        public string? Remark { get; set; }
        
        [JsonPropertyName("CreateBy")]
        public string? CreateBy { get; set; }
        
        [JsonPropertyName("LoadOnTruckStatusID")]
        public string? LoadOnTruckStatusID { get; set; }
        
        [JsonPropertyName("LoadOnTruckStatusText")]
        public string? LoadOnTruckStatusText { get; set; }
        
        [JsonPropertyName("LoadOnTruckActionDate")]
        public string? LoadOnTruckActionDate { get; set; }
        
        [JsonPropertyName("LoadOnTruckFinishDate")]
        public string? LoadOnTruckFinishDate { get; set; }
        
        [JsonPropertyName("LoadOnTruckActionBy")]
        public string? LoadOnTruckActionBy { get; set; }
        
        [JsonPropertyName("LoadOnTruckFinishBy")]
        public string? LoadOnTruckFinishBy { get; set; }
        
        [JsonPropertyName("LoadOnTruckFinishRemark")]
        public string? LoadOnTruckFinishRemark { get; set; }
        
        [JsonPropertyName("LoadOnTruckRemark")]
        public string? LoadOnTruckRemark { get; set; }
        
        [JsonPropertyName("FinishPreLoadStatus")]
        public string? FinishPreLoadStatus { get; set; }
        
        [JsonPropertyName("FinishPreLoadActionDate")]
        public string? FinishPreLoadActionDate { get; set; }
        
        [JsonPropertyName("PreLoadStatusID")]
        public string? PreLoadStatusID { get; set; }
        
        [JsonPropertyName("PreLoadStatusText")]
        public string? PreLoadStatusText { get; set; }
        
        [JsonPropertyName("PreCoolStatusText")]
        public string? PreCoolStatusText { get; set; }
        
        [JsonPropertyName("PreLoadRemark")]
        public string? PreLoadRemark { get; set; }

        [JsonPropertyName("FinishLoadStatus")]
        public string? FinishLoadStatus { get; set; }

        [JsonPropertyName("FinishLoadStatusText")]
        public string? FinishLoadStatusText { get; set; }
        }
}
