using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace GameOrganizer.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(IChatService chatService, UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }

        /// <summary>
        /// Wysyła wiadomość do konkretnej grupy.
        /// </summary>
        public async Task SendMessageToGroup(int groupId, string content)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;

            var saved = await _chatService.SaveMessageAsync(user.Id, content, groupId);

            var dto = new ChatMessageDto
            {
                Id = saved.Id,
                Content = saved.Content,
                Timestamp = saved.Timestamp,
                SenderId = user.Id,
                SenderName = user.UserName!,
                GroupId = groupId
            };

            await Clients.Group(groupId.ToString()).SendAsync("ReceiveMessage", dto);
        }


        public async Task SubscribeToMessages(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        public async Task UnsubscribeFromMessages(int groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
        }


        /// <summary>
        /// Dodaje inną osobę do konwersacji 
        /// </summary>
        public async Task InviteUserToChat(int groupId, string targetUserId)
        {
            var currentUserId = Context.UserIdentifier;

            var result = await _chatService.AddUserToGroupAsync(groupId, currentUserId!, targetUserId);

            if (result.IsSuccess)
            {
                var inviter = await _userManager.FindByIdAsync(currentUserId!);
                var target = await _userManager.FindByIdAsync(targetUserId);

                await Clients.Group(groupId.ToString()).SendAsync("UserJoined", new
                {
                    groupId = groupId,
                    username = target?.UserName,
                    message = $"{target?.UserName} został dodany przez {inviter?.UserName}"
                });

                await Clients.User(targetUserId).SendAsync("NewChatAssigned", groupId);
            }
        }

        /// <summary>
        /// Powoduje, że obecny użytkownik opuszcza konwersację
        /// </summary>
        public async Task LeaveConversation(int groupId)
        {
            var currentUserId = Context.UserIdentifier;

            var result = await _chatService.RemoveUserFromGroupAsync(groupId, currentUserId!);

            if (result.IsSuccess)
            {
                var user = await _userManager.FindByIdAsync(currentUserId!);

                await Clients.Group(groupId.ToString()).SendAsync("UserLeft", new
                {
                    groupId = groupId,
                    username = user?.UserName,
                    message = $"{user?.UserName} opuścił czat"
                });

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
            }
        }
    }
}