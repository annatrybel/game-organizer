using GameOrganizer.Api.Models.DatabaseModels;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatMessage> SaveMessageAsync(string senderId, string content, int? groupId, string? receiverId);
        Task<IEnumerable<ChatMessage>> GetHistoryAsync(int groupId);
    }
}
