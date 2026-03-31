using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_ReportRSUIn
    {
        public string Barcode { get; set; }
        public string License { get; set; }
        public string DriverName { get; set; }
        public string TruckTypeName { get; set; }
        public string RSUStatus { get; set; }
        public string RSUInDate { get; set; }
        public string RSUOutDate { get; set; }
        public string Remark { get; set; }
        

    }
}
