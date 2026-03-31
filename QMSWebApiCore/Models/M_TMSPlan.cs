using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QMSWebApiCore.Models
{
    public class M_TMSPlan
    {
        [JsonPropertyName("planNo")]
        public string? PlanNo { get; set; }

        [JsonPropertyName("planPrefix")]
        public string? PlanPrefix { get; set; }

        [JsonPropertyName("planTypeBigC")]
        public string? PlanTypeBigC { get; set; }

        [JsonPropertyName("planGroupNo")]
        public string? PlanGroupNo { get; set; }

        [JsonPropertyName("planLoadNo")]
        public string? PlanLoadNo { get; set; }

        [JsonPropertyName("planDockNo")]
        public string? PlanDockNo { get; set; }

        [JsonPropertyName("planActionDate")]
        public string? PlanActionDate { get; set; }

        [JsonPropertyName("planStoreCode")]
        public string? PlanStoreCode { get; set; }

        [JsonPropertyName("planStoreName")]
        public string? PlanStoreName { get; set; }

        [JsonPropertyName("planStoreFormat")]
        public string? PlanStoreFormat { get; set; }

        [JsonPropertyName("planTruckType")]
        public string? PlanTruckType { get; set; }

        [JsonPropertyName("planPreCool")]
        public string? PlanPreCool { get; set; }

        [JsonPropertyName("planOnDock")]
        public string? PlanOnDock { get; set; }

        [JsonPropertyName("planStartLoad")]
        public string? PlanStartLoad { get; set; }

        [JsonPropertyName("planFinishLoad")]
        public string? PlanFinishLoad { get; set; }

        [JsonPropertyName("planDispatch")]
        public string? PlanDispatch { get; set; }

        //[Required]
        [JsonPropertyName("createBy")]
        public string? CreateBy { get; set; }

        [JsonPropertyName("updateBy")]
        public string? UpdateBy { get; set; }

        [JsonPropertyName("dcCode")]
        public string? DCCode { get; set; }

        [JsonPropertyName("flag")]
        public string? Flag { get; set; } = "1";

        // Properties required by Dapper for SaveTMSPlanAsync Step 2 & 3
        [JsonPropertyName("planDate")] public string? PlanDate { get; set; }
        [JsonPropertyName("countStore")] public long? CountStore { get; set; } // COUNT returns long in PGSQL
        [JsonIgnore] public string? PreCoolStatus { get; set; }
        [JsonIgnore] public string? PreLoadStatus { get; set; }
        [JsonIgnore] public string? TruckOnDockStatus { get; set; }
        [JsonIgnore] public string? StartLoadStatus { get; set; }
        [JsonIgnore] public string? FinishLoadStatus { get; set; }
        [JsonIgnore] public string? DispatchStatus { get; set; }
        [JsonIgnore] public string? PlanStatus { get; set; }
        [JsonIgnore] public string? Group { get; set; }
        [JsonIgnore] public string? Dock { get; set; }
        [JsonIgnore] public string? Load { get; set; }
        [JsonIgnore] public DateTime? CreatedDate { get; set; }
    }
}
