using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class ApplicationUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
    }
}
