using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_Dashboard
    {
        public string? Result { get; set; }
        public string? ErrorMSG { get; set; }
        public string? PlanActionDate { get; set; }
        public string? PlanBigCType { get; set; }
        public string? PlanGroupNo { get; set; }
        public string? PlanCount { get; set; }
        public string? PlanFinish { get; set; }
        public string? PlanRemain { get; set; }
        public string? PlanFinishLoad { get; set; }
        public string? PlanFinishLoadTime { get; set; }
        public string? DCCode { get; set; }
    }

}
