using System.ComponentModel.DataAnnotations;

namespace GameOrganizer.Api.Models.Dto
{
    public class RegisterDto
    {
        [Required]
    [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_\-]*$", ErrorMessage = "Username zawiera niedozwolone znaki.")]
        public string Username { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
