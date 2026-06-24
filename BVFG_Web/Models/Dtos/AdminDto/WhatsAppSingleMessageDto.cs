using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BVFG_Web.Models.Dtos.AdminDto
{
    public class WhatsAppSingleMessageDto
    {
        [Required(ErrorMessage = "Mobile number is required")]
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
