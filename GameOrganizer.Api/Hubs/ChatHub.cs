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

       
    }
}