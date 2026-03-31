using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameOrganizer.Api.Controllers
{
    [Authorize]
    [Route("api/chat")]
    public class ChatController : GameOrganizerBaseController
    {
        private readonly IChatService _chatService;
        public ChatController(IChatService chatService) => _chatService = chatService;

        /// <summary>
        /// Pobiera listę wszystkich konwersacji zalogowanego użytkownika.
        /// </summary>
        /// <remarks>
        /// Zwraca informacje o grupach, ostatnich wiadomościach oraz listę uczestników.
        /// </remarks>
        /// <returns>Lista obiektów ChatGroupDto.</returns>
        [HttpGet("my-chats")]
        [ProducesResponseType(typeof(IEnumerable<ChatGroupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyChats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _chatService.GetUserChatsAsync(userId!));
        }

        /// <summary>
        /// Pobiera historię wiadomości dla konkretnej grupy czatowej.
        /// </summary>
        /// <param name="groupId">Unikalny identyfikator grupy (czatu).</param>
        /// <returns>Lista wiadomości w formacie ChatMessageDto.</returns>
        /// <response code="200">Zwraca historię wiadomości.</response>
        /// <response code="403">Użytkownik nie należy do tej grupy.</response>
        [HttpGet("{groupId}/messages")]
        [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMessages(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _chatService.GetChatHistoryAsync(groupId, userId!));
        }

        /// <summary>
        /// Tworzy nową konwersację.
        /// </summary>
        /// <remarks>
        /// Jeśli w liście UserIds znajduje się tylko jeden użytkownik, system sprawdzi, 
        /// czy konwersacja 1-na-1 już istnieje i zwróci jej ID zamiast tworzyć nową.
        /// </remarks>
        /// <param name="request">Model zawierający opcjonalną nazwę grupy oraz listę ID zaproszonych użytkowników.</param>
        /// <returns>ID nowo utworzonej lub istniejącej grupy czatowej.</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateChatRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _chatService.CreateChatAsync(userId!, request));
        }
    }
}