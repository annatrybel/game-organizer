using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using GameOrganizer.Api.Services.Interfaces;

namespace GameOrganizer.Api.Hubs
{
    [Authorize] // Wymaga JWT
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(string content, string? receiverId, int? groupId)
        {
            var senderId = Context.UserIdentifier; 

            var savedMessage = await _chatService.SaveMessageAsync(senderId, content, groupId, receiverId);

            if (groupId.HasValue)
            {
                await Clients.Group(groupId.Value.ToString()).SendAsync("ReceiveMessage", savedMessage);
            }
            else if (!string.IsNullOrEmpty(receiverId))
            {
                await Clients.Users(senderId, receiverId).SendAsync("ReceiveMessage", savedMessage);
            }
        }

        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }
    }
}