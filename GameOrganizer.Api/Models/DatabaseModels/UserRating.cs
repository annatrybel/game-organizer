namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class UserRating
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int GameId { get; set; }
        public int Value { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Game Game { get; set; } = null!;
    }
}
