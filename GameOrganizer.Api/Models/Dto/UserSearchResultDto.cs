namespace GameOrganizer.Api.Models.Dto
{
    public class UserSearchResultDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string RelationStatus { get; set; } = "None";
    }
}
