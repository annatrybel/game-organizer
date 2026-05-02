namespace GameOrganizer.Api.Models.Dto
{
    public class SharedCollectionDto
    {
        public string CollectionName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public List<UserGameDto> Games { get; set; } = new();
    }
}
