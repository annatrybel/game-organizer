using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class ApplicationUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string InvitationCode { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
