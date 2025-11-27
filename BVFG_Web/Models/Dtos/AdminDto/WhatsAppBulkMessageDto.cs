using System.ComponentModel.DataAnnotations;

namespace BVFG_Web.Models.Dtos.AdminDto
{
    public class WhatsAppBulkMessageDto
    {
        [Required(ErrorMessage = "Excel file is required")]
        public IFormFile ExcelFile { get; set; }

        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; }

        [Range(1, 60, ErrorMessage = "Delay must be between 1 and 60 seconds")]
        public int DelaySeconds { get; set; } = 5;
    }
}
