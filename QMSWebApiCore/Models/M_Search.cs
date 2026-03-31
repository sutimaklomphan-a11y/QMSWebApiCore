namespace QMSWebApiCore.Models
{
    public class M_Search
    {
        public string Search { get; set; }

        public string DCCode { get; set; }
        public string Barcode { get; set; }
        public string TruckTypeID { get; set; }
        public string PlanNo { get; set; }

        public string PlanGroupNo { get; set; }

        public string PlanBigCType { get; set; }
        public string PlanActionDate { get; set; }
        public string PlanDate { get; set; }
        public string PlanPreCoolStatusID { get; set; }
        public string PreCoolStatusID { get; set; }

        public string PreLoadStatusID { get; set; }

        public string FinishPreLoadStatusID { get; set; }

        public string PlanTruckOnDockStatusID { get; set; }
        public string TruckOnDockStatusID { get; set; }

        public string LoadOnTruckStatusID { get; set; }
        public string FinishLoadStatusID { get; set; }

        public string EDPStatusID { get; set; }
        public string LoadOnTruck { get; set; }

        public string DashboardStatus { get; set; }
    }
}
