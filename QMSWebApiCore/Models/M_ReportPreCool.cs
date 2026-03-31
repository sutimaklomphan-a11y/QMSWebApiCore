using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboundQMSModel
{
    public class M_ReportPreCool
    {
        public string PreCoolDate { get; set; }
        public string BigCType { get; set; }
        public string Group { get; set; }
        public string Barcode { get; set; }
        public string LoadNo { get; set; }
        public string DockNo { get; set; }        
        public string DriverName { get; set; }
        public string LicenseTruck { get; set; }
        public string TruckTypeName { get; set; }
        public string QCID { get; set; }
        public string PreCoolStatus { get; set; }

        public string PreCoolActionTime { get; set; }

        public string LicenseContainer { get; set; }
        public string TempTruck { get; set; }
        public string TempContainer { get; set; }

        public string Remark { get; set; }
        public string StoreCode{ get; set; }
        public string StoreName { get; set; }



    }
}
