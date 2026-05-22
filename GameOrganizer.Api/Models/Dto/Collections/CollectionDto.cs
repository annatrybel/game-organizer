namespace GameOrganizer.Api.Models.Dto.Collections
{
    public class CollectionDto
    {
        public int? Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }
}
