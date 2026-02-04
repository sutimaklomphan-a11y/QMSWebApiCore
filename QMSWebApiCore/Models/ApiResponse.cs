namespace QMSWebApiCore.Models
{
    public class ApiResponse<T>
    {
        public bool Result { get; set; }
        public string ErrorMessage { get; set; }
        public string TextFocus { get; set; }
        public T Data { get; set; }
    }
}
