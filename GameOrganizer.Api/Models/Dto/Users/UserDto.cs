using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GameOrganizer.Api.Models.Dto.Users
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        [Required]
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public string InvitationCode { get; set; }
        public List<IdentityRole> AvailableRoles { get; set; } = new List<IdentityRole>();
    }

    public class UpdateProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public IFormFile? Avatar { get; set; }
        [StringLength(500)]
        public string? Bio { get; set; }
    }
}