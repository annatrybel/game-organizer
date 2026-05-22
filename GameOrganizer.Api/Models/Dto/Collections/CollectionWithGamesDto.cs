using GameOrganizer.Api.Models.Dto.Users;

namespace GameOrganizer.Api.Models.Dto.Collections
{
    public class CollectionWithGamesDto
    {
        public int CollectionId { get; set; }
        public string CollectionName { get; set; }
        public bool IsPublic { get; set; }
        public List<UserGameDto> Games { get; set; } = new();
    }
}
