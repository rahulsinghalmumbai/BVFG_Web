namespace BVFG_Web.Models.AdminModel
{
    public class WhatsAppSendModel
    {
        public string Mobile { get; set; } 
        public string Message { get; set; } 
        public IFormFile ExcelFile { get; set; }
        public int DelaySeconds { get; set; } = 5;
    }
}
