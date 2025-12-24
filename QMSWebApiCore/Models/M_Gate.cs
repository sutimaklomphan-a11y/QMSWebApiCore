namespace QMSWebApiCore.Models
{
    public class M_Gate
    {
        public string Result { get; set; }
        public string ErrorMessage { get; set; }
        public string GateInDate { get; set; }
        public string GateOutDate { get; set; }
        public string DriverName { get; set; }
        public string TruckTypeID { get; set; }
        public string TruckTypeName { get; set; }
        public string TruckTypeText { get; set; }

        public string TruckTypeStyle { get; set; }
        public string TruckTypeImage { get; set; }
        public string LicenseTruck { get; set; }
        public string ActionBy { get; set; }
        public string ActionDate { get; set; }
        public string RSUStatusID { get; set; }
        public string RSUStatusText { get; set; }
        public string Barcode { get; set; }
        public string GateInRemark { get; set; }
        public string GateOutRemark { get; set; }
        public string LPSName { get; set; }
        public bool GateStatus { get; set; }
        public string GateStatusText { get; set; }
        public string GateDCCode { get; set; }
        public string CreateBy { get; set; }
        public string GateID { get; set; }
    }
}