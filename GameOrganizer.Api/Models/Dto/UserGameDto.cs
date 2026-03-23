namespace GameOrganizer.Api.Models.Dto
{
    public class UserGameDto
    {
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string GenreName { get; set; } = string.Empty;
        public string PlatformName { get; set; } = string.Empty;

        public int CollectionId { get; set; }
        public string CollectionName { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }
}
