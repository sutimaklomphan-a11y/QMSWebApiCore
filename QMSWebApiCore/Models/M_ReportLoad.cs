using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_ReportLoad
    {
        public string LoadActionTime { get; set; }
        public string FinishLoadActionTime { get; set; }
        public string Barcode { get; set; }

        public string BigCType { get; set; }
        public string Group { get; set; }        
        public string LoadNo { get; set; }
        public string DockNo { get; set; }        
        public string LicenseTruck { get; set; }
        public string TruckTypeName { get; set; }
        public string Loader { get; set; }
        public string LoadStatus { get; set; }
        public string Remark { get; set; }

        public string FinishLoadRemark { get; set; }



    }
}
