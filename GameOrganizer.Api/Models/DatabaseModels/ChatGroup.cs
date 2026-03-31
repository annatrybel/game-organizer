namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class ChatGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<ChatGroupMember> Members { get; set; } = new List<ChatGroupMember>();
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
