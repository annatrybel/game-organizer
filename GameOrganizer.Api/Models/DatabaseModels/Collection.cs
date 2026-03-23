using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class Collection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true; 
        public Guid ShareCode { get; set; } = Guid.NewGuid();

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public ICollection<UserGame> UserGames { get; set; } = new List<UserGame>();
    }
}
