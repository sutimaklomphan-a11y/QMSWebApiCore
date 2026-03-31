using System.Text.Json.Serialization;

namespace QMSWebApiCore.Models
{
    public class M_TruckOnDock
    {
        [JsonPropertyName("DCCode")]
        public string DCCode { get; set; }
        
        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }
        
        [JsonPropertyName("BarcodeNew")]
        public string? BarcodeNew { get; set; }
        
        [JsonPropertyName("planNo")]
        public string? PlanNo { get; set; }
        
        [JsonPropertyName("PlanActionDate")]
        public string? PlanActionDate { get; set; }
        
        [JsonPropertyName("PlanDate")]
        public string? PlanDate { get; set; }

        [JsonPropertyName("PlanBigCType")]
        public string? PlanBigCType { get; set; }
        
        [JsonPropertyName("PlanLoadNo")]
        public string? PlanLoadNo { get; set; }
        
        [JsonPropertyName("PlanGroupNo")]
        public string? PlanGroupNo { get; set; }
        
        [JsonPropertyName("PlanDockNo")]
        public string? PlanDockNo { get; set; }
        
        [JsonPropertyName("LicenseTruck")]
        public string? LicenseTruck { get; set; }
        
        [JsonPropertyName("TruckTypeID")]
        public string? TruckTypeID { get; set; }
        
        [JsonPropertyName("TruckTypeText")]
        public string? TruckTypeText { get; set; }
        
        [JsonPropertyName("TruckTypeImage")]
        public string? TruckTypeImage { get; set; }
        
        [JsonPropertyName("TruckOnDockRemark")]
        public string? TruckOnDockRemark { get; set; }
        
        [JsonPropertyName("TruckOnDockReviseRemark")]
        public string? TruckOnDockReviseRemark { get; set; }
        
        [JsonPropertyName("CreateBy")]
        public string? CreateBy { get; set; }
        
        [JsonPropertyName("ActionDate")]
        public string? ActionDate { get; set; }
        
        [JsonPropertyName("PrecoolStatus")]
        public string? PrecoolStatus { get; set; }
        
        [JsonPropertyName("PrecoolStatusText")]
        public string? PrecoolStatusText { get; set; }
        
        [JsonPropertyName("TruckOnDockStatusID")]
        public string? TruckOnDockStatusID { get; set; }
        
        [JsonPropertyName("TruckOnDockStatus")]
        public string? TruckOnDockStatus { get; set; }
        
        [JsonPropertyName("ActualTruckOnDockTime")]
        public string? ActualTruckOnDockTime { get; set; }
        
        [JsonPropertyName("EDPINStatus")]
        public string? EDPINStatus { get; set; }
        
        [JsonPropertyName("EDPOutStatus")]
        public string? EDPOutStatus { get; set; }
        
        [JsonPropertyName("Search")]
        public string? Search { get; set; }
    }
}
