namespace GameOrganizer.Api.Models.DatabaseModels
{
    public enum FriendshipStatus { Pending, Accepted }

    public class Friendship
    {
        public int Id { get; set; }

        public string RequesterId { get; set; } = null!;
        public ApplicationUser Requester { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
