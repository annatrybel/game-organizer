using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IFriendService
    {
        Task<ServiceResult> SendFriendRequestAsync(string requesterId, string targetUsername);
        Task<ServiceResult<IEnumerable<FriendDto>>> GetFriendsAsync(string userId);
        Task<ServiceResult> SendInviteEmailAsync(string userId, string recipientEmail);
    }
}
