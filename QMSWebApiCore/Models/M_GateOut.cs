namespace QMSWebApiCore.Models
{
    public class M_GateOut
    {
        public int Gate_id { get; set; }
        public string? GateInDate { get; set; }
        public string? GateOutDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ActionBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? LicenseTruck { get; set; }
        public int RSUStatusID { get; set; }
        public int RSUID { get; set; }
        public string? EDPStatusID { get; set; }
        public string? RSUStatusText { get; set; }
        public string? Barcode { get; set; }
        public string? GateInRemark { get; set; }
        public string? GateOutRemark { get; set; }
        public string? DriverName { get; set; }
        public int GateStatus { get; set; }
        public int TruckTypeID { get; set; }
        public string? TruckTypeText { get; set; }
        public string? TruckTypeName { get; set; }
        public string? TruckTypeStyle { get; set; }
        public string? TruckTypeImage { get; set; }
        public int? GateID { get; set; }
        public string? DCCode { get; set; }
        public Boolean flag { get; set; }
        public string? gate_status_text { get; set; }
        public string? GateInDateTime { get; set; }
        public string? RSUInDateTime { get; set; }
        public string? RSUOutDateTime { get; set; }
        public string? TruckOnDockDate { get; set; }
        public string? PreLoadDate { get; set; }
        public string? LoadInTruckDate { get; set; }
        public string? LoadInTruckFinishDate { get; set; }
        public string? License { get; set; }
        public string? PlanDate { get; set; }
        public string? PlanLoadNo { get; set; }
        public string? PlanGroupNo { get; set; }
        public string? PlanDockNo { get; set; }
        public string? LastProcess { get; set; }
        public string? EDPInBy { get; set; }
        public string? EDPInDate { get; set; }
        public string? EDPInRemark { get; set; }
        public string? EDPOutBy { get; set; }
        public string? EDPOutDate { get; set; }
        public string? EDPOutRemark { get; set; }
        public string? PlanNo { get; set; }
    }
}
