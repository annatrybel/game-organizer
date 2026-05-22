namespace GameOrganizer.Api.Models.Dto.Games
{
    public class GameDisplayDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
        public int PlatformId { get; set; }
        public string PlatformName { get; set; } = string.Empty; 
        public string? ImageUrl { get; set; } 
        public double AverageRating { get; set; } 
        public int? MyRating { get; set; }       
    }
}
