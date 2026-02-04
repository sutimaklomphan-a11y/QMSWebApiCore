namespace QMSWebApiCore.Models
{
    public class M_Shipping
    {
        float? shippingAmount;
    }
    public class M_LoadOnTruck
    {
        public string Result { get; set; }
        public string ErrorMessage { get; set; }
        public string PlanDate { get; set; }
        public string PlanNo { get; set; }
        public string GroupNo { get; set; }
        public string LoadNo { get; set; }
        public string DockNo { get; set; }
        public string LoadOnTruckActionDate { get; set; }
        public string LoadOnTruckActionBy { get; set; }
        public string LoadOnTruckStatusID { get; set; }
        public string LoadOnTruckStatusText { get; set; }
        public string LoadOnTruckRemark { get; set; }
        public string Employee1 { get; set; }
        public string Employee2 { get; set; }
        public string Employee3 { get; set; }
        public string Employee4 { get; set; }
        public string Employee5 { get; set; }
        public string Employee6 { get; set; }
        public string Employee7 { get; set; }
        public string Employee8 { get; set; }
        public string Employee9 { get; set; }
        public string Employee10 { get; set; }
    public class M_PreLoad_
    {
        public string DCCode { get; set; }
        public string PlanDate { get; set; }
        public string PlanNo { get; set; }
        public string GroupNo { get; set; }
        public string LoadNo { get; set; }
        public string DockNo { get; set; }
        public string PreLoadActionDate { get; set; }
        public string PreLoadActionBy { get; set; }
        public string Employee1 { get; set; }
        public string Employee2 { get; set; }
        public string Employee3 { get; set; }
        public string Employee4 { get; set; }
        public string Employee5 { get; set; }
        public string Employee6 { get; set; }
        public string Employee7 { get; set; }
        public string Employee8 { get; set; }
        public string Employee9 { get; set; }
        public string Employee10 { get; set; }
        public string PreCoolStatusID { get; set; }
        public string PreCoolStatusText { get; set; }
        public string PreLoadStatusID { get; set; }
        public string PreLoadStatusText { get; set; }
        public string PreLoadRemark { get; set; }
    }
    public class M_FinishPreLoad
    {
        public string DCCode { get; set; }
        public string PlanDate { get; set; }
        public string PlanNo { get; set; }
        public string PlanGroupNo { get; set; }
        public string LoadNo { get; set; }
        public string DockNo { get; set; }
        public string PreLoadStatusID { get; set; }
        public string FinishPreLoadActionDate { get; set; }
        public string FinishPreLoadActionBy { get; set; }
        public string FinishPreLoadStatusID { get; set; }
        public string FinishPreLoadStatusText { get; set; }
        public string FinishPreLoadRemark { get; set; }
    }
    public class M_LoadInTruck
    {
            public string DCCode { get; set; }
            public string PlanNo { get; set; }
            public string PlanDate { get; set; }
            public string PlanGroupNo { get; set; }
            public string LoadNo { get; set; }
            public string ActionBy { get; set; }
            public string ActionDate { get; set; }
            public string DockNo { get; set; }
            public string Employee1 { get; set; }
            public string Employee2 { get; set; }
            public string Employee3 { get; set; }
            public string Employee4 { get; set; }
            public string Employee5 { get; set; }
            public string Employee6 { get; set; }
            public string Employee7 { get; set; }
            public string Employee8 { get; set; }
            public string Employee9 { get; set; }
            public string Employee10 { get; set; }
            public string Remark { get; set; }
            public string CreateBy { get; set; }
            public string LoadOnTruckStatusID { get; set; }
            public string LoadOnTruckStatusText { get; set; }
            public string LoadOnTruckActionDate { get; set; }
            public string LoadOnTruckFinishDate { get; set; }
            public string LoadOnTruckActionBy { get; set; }
            public string LoadOnTruckFinishBy { get; set; }
            public string LoadOnTruckFinishRemark { get; set; }
            public string LoadOnTruckRemark { get; set; }
        }
    }
}
