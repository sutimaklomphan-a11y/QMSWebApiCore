using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_ReportGateOut
    {
        public string Barcode { get; set; }
        public string License { get; set; }
        public string DriverName { get; set; }
        public string TruckTypeName { get; set; }
        public string RSUStatus { get; set; }
        public string GateInDate { get; set; }
        public string GateOutDate { get; set; }
        public string LPSName { get; set; }
        public string Remark { get; set; }

    }
}
