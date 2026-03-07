using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class UserGame
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
