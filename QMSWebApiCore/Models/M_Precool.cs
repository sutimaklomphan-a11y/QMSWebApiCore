using System.Text;
using System.Threading.Tasks;

namespace QMSWebApiCore.Models
{
    public class M_Precool
    {
        public string DCCode { get; set; }
        public string PlanNo { get; set; }
        public string PlanGroupNo { get; set; }
        public string PlanLoadNo { get; set; }
        public string ActionDate { get; set; }
        public string PlanDockNo { get; set; }
        public string QCID { get; set; }
        public string LicenseTruck { get; set; }
        public string LicenseBehind { get; set; }
        public string TempuratureFront { get; set; }
        public string TempuratureBehind { get; set; }
        public string Remark { get; set; }
        public string RemarkPass { get; set; }
        public string CreateBy { get; set; }
        public string PlanPreCoolStatusID { get; set; }
        public string StoreCode { get; set; }
        public string StoreFormat { get; set; }
        public string StoreName { get; set; }

    }

    public class M_PreCool_Fail
    {
        public string PlanNo { get; set; }
        public string QCID { get; set; }
        public string PreCoolActionDate { get; set; }
        public string LicenseContainer { get; set; }
        public string TempulatorTruck { get; set; }
        public string TempulatorContainer { get; set; }
        public string Remark { get; set; }
    }
}
