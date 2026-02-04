namespace QMSWebApiCore.Models
{
    public class M_Login
    {
        public bool? Result { get; set; }
        public string ErrorMessage { get; set; }
        public string AccountCode { get; set; }
        public string AccountUsername { get; set; }
        public string AccountPassword { get; set; }
        public string AccountName { get; set; }
        public string AccountDCCode { get; set; }
    }
}
