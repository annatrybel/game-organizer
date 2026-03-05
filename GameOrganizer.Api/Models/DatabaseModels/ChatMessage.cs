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
        public IdentityUser Sender { get; set; }

        // Indywidualny czat
        public string? ReceiverId { get; set; }
        public IdentityUser? Receiver { get; set; }

        // Grupowy czat
        public int? GroupId { get; set; }
        public ChatGroup? Group { get; set; }
    }

    public class ChatGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<IdentityUser> Members { get; set; } = new List<IdentityUser>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
