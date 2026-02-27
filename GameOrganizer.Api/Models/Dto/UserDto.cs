using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GameOrganizer.Api.Models.Dto
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public List<IdentityRole> AvailableRoles { get; set; } = new List<IdentityRole>();
    }
}