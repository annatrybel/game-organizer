using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Nadawca
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        // Indywidualny czat
        public string? ReceiverId { get; set; }
        public ApplicationUser? Receiver { get; set; }

        // Grupowy czat
        public int? GroupId { get; set; }
        public ChatGroup? Group { get; set; }
    }

    public class ChatGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
