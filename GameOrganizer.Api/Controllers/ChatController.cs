using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameOrganizer.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : GameOrganizerBaseController
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("history/group/{groupId}")]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var history = await _chatService.GetHistoryAsync(groupId);
            return Ok(history);
        }
    }
}