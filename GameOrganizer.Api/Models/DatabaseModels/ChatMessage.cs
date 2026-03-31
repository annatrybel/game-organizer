using Microsoft.AspNetCore.Identity;

namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
       
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
       
        public int GroupId { get; set; }
        public ChatGroup Group { get; set; }
    }
}
