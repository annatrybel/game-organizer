using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{    
    public class Game
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int GenreId { get; set; }
        public Genre Genre { get; set; }
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        public string? ImageUrl { get; set; }
    }
}
