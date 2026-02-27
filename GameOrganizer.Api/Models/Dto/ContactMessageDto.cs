using System.ComponentModel.DataAnnotations;

namespace GameOrganizer.Api.Models.ViewModel
{
    public class ContactMessageDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? Message { get; set; }
    }
}