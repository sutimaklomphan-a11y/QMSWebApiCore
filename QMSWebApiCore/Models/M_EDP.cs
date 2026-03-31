namespace QMSWebApiCore.Models
{
    public class M_EDP
    {
        public string? DCCode { get; set; }
        public string? Result { get; set; }
        public string? ErrorMSG { get; set; }
        public string? Barcode { get; set; }
        public string? PlanNo { get; set; }
        public string? PlanDate { get; set; }
        public string? PlanBigCType { get; set; }
        public string? PlanGroupNo { get; set; }
        public string? PlanLoadNo { get; set; }
        public string? PlanDockNo { get; set; }
        public string? LicenseTruck { get; set; }
        public string? DriverName { get; set; }
        public string? TruckTypeID { get; set; }
        public string? TruckTypeName { get; set; }
        public string? TruckTypeImage { get; set; }

        // Action time
        public string? GateInDate { get; set; }
        public string? RSUInDate { get; set; }
        public string? RSUOutDate { get; set; }
        public string? TruckOnDockDate { get; set; }
        public string? PreLoadDate { get; set; }
        public string? StartLoadDate { get; set; }
        public string? FinishLoadDate { get; set; }
        public string? EDPInDate { get; set; }
        public string? EDPOutDate { get; set; }

        // Remark
        public string? EDPRemarkIn { get; set; }
        public string? EDPRemarkOut { get; set; }

        // Status
        public string? EDPStatusID { get; set; }
        public string? EDPStatusText { get; set; }
        public string? EDPInActionBy { get; set; }
        public string? EDPOutActionBy { get; set; }
        public string? LPSName { get; set; }
        public string? LastProcess { get; set; }

        // Search
        public string? Search { get; set; }
    }
}
