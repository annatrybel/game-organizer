using GameOrganizer.Api.Models.Dto.Users;

namespace GameOrganizer.Api.Models.Dto.Collections
{
    public class SharedCollectionDto
    {
        public string CollectionName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public List<UserGameDto> Games { get; set; } = new();
    }
}
