// Models/GateIn.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QMSWebApiCore.Models
{
    public class M_GateIn
    {
        public int Gate_id { get; set; }
        public DateTime? GateInDate { get; set; }
        public DateTime? GateOutDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ActionBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? LicenseTruck { get; set; }
        public int RSUStatusID { get; set; }
        public int RSUID { get; set; }
        public string? EDPStatusID { get; set; }
        public string? RSUStatusText { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? GateInRemark { get; set; }
        public string? GateOutRemark { get; set; }
        public string? DriverName { get; set; }
        public int GateStatus { get; set; }
        public int TruckTypeID { get; set; }
        public string? TruckTypeText { get; set; }
        public string? TruckTypeName { get; set; }
        public string? TruckTypeStyle { get; set; }
        public string? TruckTypeImage { get; set; }
        public int? GateID { get; set; }
        public string? DCCode { get; set; }
        public Boolean flag { get; set; }
        public string? gate_status_text { get; set; }
    }
}