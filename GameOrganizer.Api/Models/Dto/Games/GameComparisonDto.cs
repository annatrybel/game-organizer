namespace GameOrganizer.Api.Models.Dto.Games
{
    public class GameComparisonDto
    {
        public int GameId { get; set; }
        public string Title { get; set; }
        public string GenreName { get; set; }
        public bool OwnedByMe { get; set; }
        public bool OwnedByFriend { get; set; }
        public string? MyCollectionName { get; set; }
        public string? FriendCollectionName { get; set; }
    }
}
