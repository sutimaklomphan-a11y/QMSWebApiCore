namespace QMSWebApiCore.Models
{
    public class M_EDP
    {
        public string DCCode { get; set; }
        public string Result { get; set; }
        public string ErrorMSG { get; set; }
        public string Barcode { get; set; }
        public string PlanNo { get; set; }
        public string PlanDate { get; set; }
        public string PlanBigCType { get; set; }
        public string PlanGroupNo { get; set; }
        public string PlanLoadNo { get; set; }
        public string PlanDockNo { get; set; }
        public string LicenseTruck { get; set; }
        public string DriverName { get; set; }
        public string TruckTypeID { get; set; }
        public string TruckTypeName { get; set; }
        public string TruckTypeImage { get; set; }

        //Action time
        public string GateIn { get; set; }
        public string RSUIn { get; set; }
        public string RSUOut { get; set; }
        public string TruckOnDock { get; set; }
        public string PreLoad { get; set; }
        public string StartLoad { get; set; }
        public string FinishLoad { get; set; }
        public string EDPIn { get; set; }
        public string EDPOut { get; set; }

        //Remark
        public string EDPRemarkIn { get; set; }
        public string EDPRemarkOut { get; set; }
        //Status
        public string EDPStatusID { get; set; }
        public string EDPStatusText { get; set; }
        public string EDPInActionBy { get; set; }
        public string EDPOutActionBy { get; set; }
        public string LPSName { get; set; }
        public string LastProcess { get; set; }
    }
}
