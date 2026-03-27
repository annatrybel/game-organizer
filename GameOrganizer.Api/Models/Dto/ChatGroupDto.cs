namespace GameOrganizer.Api.Models.Dto
{
    public class ChatGroupDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageTime { get; set; }
        public List<string> Participants { get; set; } = new();
    }

    public class CreateChatRequest
    {
        public string? GroupName { get; set; }
        public List<string> UserIds { get; set; } = new();
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SenderId { get; set; } = null!;  
        public string SenderName { get; set; } = string.Empty; 
        public int GroupId { get; set; }
    }
}
