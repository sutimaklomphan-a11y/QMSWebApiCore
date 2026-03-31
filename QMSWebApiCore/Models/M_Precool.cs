using System.Text.Json.Serialization;

namespace QMSWebApiCore.Models
{
    //public class M_Precool
    //{
    //    public string? Search {  get; set; } 
    //    public string? PlanBigCType { get; set; }
    //    public string DCCode { get; set; }
    //    public string? PlanNo { get; set; }
    //    public string? PlanGroupNo { get; set; }
    //    public string? PlanLoadNo { get; set; }
    //    public string? LastActionPreCoolDate { get; set; }
    //    public string? PlanActionDate { get; set; }
    //    public string? PlanDockNo { get; set; }
    //    public string? QCID { get; set; }
    //    public string? licenseTruck { get; set; }
    //    public string? licenseBehind { get; set; }
    //    public string? TempuratureFront { get; set; }
    //    public string? TempuratureBehind { get; set; }
    //    public string? Remark { get; set; }
    //    public string? RemarkPass { get; set; }
    //    public string? CreateBy { get; set; }
    //    public string? PlanPreCoolStatusID { get; set; }
    //    public string? PreCoolStatusText { get; set; }
    //    public string StoreCode { get; set; }
    //    public string StoreFormat { get; set; }
    //    public string StoreName { get; set; }
    //}

    public class M_Precool
    {
        [JsonPropertyName("DCCode")]
        public string? DCCode { get; set; }
        
        [JsonPropertyName("planNo")]
        public string? PlanNo { get; set; }
        
        [JsonPropertyName("Search")]
        public string? Search { get; set; }
        
        [JsonPropertyName("PlanGroupNo")]
        public string? PlanGroupNo { get; set; }
        
        [JsonPropertyName("PlanDate")]
        public string? PlanDate { get; set; }
        
        [JsonPropertyName("PlanBigCType")]
        public string? PlanBigCType { get; set; }
        
        [JsonPropertyName("PlanLoadNo")]
        public string? PlanLoadNo { get; set; }
        
        [JsonPropertyName("ActionDate")]
        public string? ActionDate { get; set; }
        
        [JsonPropertyName("PlanDockNo")]
        public string? PlanDockNo { get; set; }
        
        [JsonPropertyName("PlanPreCool")]
        public string? PlanPreCool { get; set; }
        
        [JsonPropertyName("LastActionPreCool")]
        public string? LastActionPreCool { get; set; }
        
        [JsonPropertyName("PlanPreCoolCountNotPass")]
        public string? PlanPreCoolCountNotPass { get; set; }
        
        [JsonPropertyName("QCID")]
        public string? QCID { get; set; }
        
        [JsonPropertyName("LicenseTruck")]
        public string? LicenseTruck { get; set; }
        
        [JsonPropertyName("LicenseBehind")]
        public string? LicenseBehind { get; set; }
        
        [JsonPropertyName("TempuratureFront")]
        public string? TempuratureFront { get; set; }
        
        [JsonPropertyName("TempuratureBehind")]
        public string? TempuratureBehind { get; set; }
        
        [JsonPropertyName("Remark")]
        public string? Remark { get; set; }
        
        [JsonPropertyName("RemarkPass")]
        public string? RemarkPass { get; set; }
        
        [JsonPropertyName("CreateBy")]
        public string? CreateBy { get; set; }
        
        [JsonPropertyName("PreCoolStatusID")]
        public string? PreCoolStatusID { get; set; }
        
        [JsonPropertyName("PlanPreCoolStatusID")]
        public string? PlanPreCoolStatusID { get; set; }
        
        [JsonPropertyName("StoreCode")]
        public string? StoreCode { get; set; }
        
        [JsonPropertyName("StoreFormat")]
        public string? StoreFormat { get; set; }
        
        [JsonPropertyName("StoreName")]
        public string? StoreName { get; set; }
    }
}
