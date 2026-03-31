using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_TimeLineDashboard
    {
        public string Result { get; set; }
        public string ErrorMSG { get; set; }

        public string PlanNo { get; set; }
        public string PlanActionDate { get; set; }
        public string PlanGroupNo { get; set; }
        public string PlanDockNo { get; set; }
        public string PlanLoadNo { get; set; }
        public string PlanBigCType { get; set; }
        public string PlanStatusID { get; set; }
        public string PlanStatusText { get; set; }

        //Truck
        public string TruckTypeID { get; set; }
        public string TruckTypeText { get; set; }
        public string TruckLicense { get; set; }
        public string DriverName { get; set; }

        //TimeLine
        public string PreCoolStatusID { get; set; }
        public string PreCoolStatusText { get; set; }
        public string PreCoolActionTime { get; set; }
        public string PreCoolDelayTime { get; set; }

        public string TruckOnDockStatusID { get; set; }
        public string TruckOnDockStatusText { get; set; }
        public string TruckOnDockActionTime { get; set; }
        public string TruckOnDockDelayTime { get; set; }

        public string StartLoadStatusID { get; set; }
        public string StartLoadStatusText { get; set; }
        public string StartLoadActionTime { get; set; }
        public string StartLoadDelayTime { get; set; }

        public string FinishLoadStatusID { get; set; }
        public string FinishLoadStatusText { get; set; }
        public string FinishLoadActionTime { get; set; }
        public string FinishLoadDelayTime { get; set; }

        public string EDPStatusID { get; set; }
        public string EDPStatusText { get; set; }
        public string EDPActionTime { get; set; }
        public string EDPDelayTime { get; set; }





    }

}
