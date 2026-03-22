namespace GameOrganizer.Api.Models.View
{
    public class UserGamesView
    {
        public int UserGameId { get; set; }
        public string UserId { get; set; } = null!;
        public int GameId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string GenreName { get; set; } = null!;
        public string PlatformName { get; set; } = null!;
        public int CollectionId { get; set; }
        public string CollectionName { get; set; } = null!;
        public DateTime AddedAt { get; set; }
    }
}
