namespace GameOrganizer.Api.Models.Dto
{
    public class GameDto
    {
        public int? Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int GenreId { get; set; }
        public IFormFile? Image { get; set; }
    }
}
