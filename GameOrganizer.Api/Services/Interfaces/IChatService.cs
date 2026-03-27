using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IChatService
    {
        Task<ServiceResult<IEnumerable<ChatGroupDto>>> GetUserChatsAsync(string userId);
        Task<ServiceResult<IEnumerable<ChatMessageDto>>> GetChatHistoryAsync(int groupId, string userId);
        Task<ServiceResult<int>> CreateChatAsync(string currentUserId, CreateChatRequest request);
        Task<ChatMessage> SaveMessageAsync(string senderId, string content, int groupId);
    }
}
