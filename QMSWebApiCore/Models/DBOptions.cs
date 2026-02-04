using Microsoft.AspNetCore.Http;

namespace QMSWebApiCore.Models
{
    public class DBOptions
    {
        public const string SectionName = "WMSPortal";
        public string Schema = "outbound_qms_sim";
        public int CommandTimeout { get; set; } = 30;
    }
}
