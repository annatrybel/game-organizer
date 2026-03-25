namespace GameOrganizer.Api.Models.Dto
{
    public class FriendDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; } 
    }
}
