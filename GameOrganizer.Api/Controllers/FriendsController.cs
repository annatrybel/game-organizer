using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameOrganizer.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/friends")]
    public class FriendsController : GameOrganizerBaseController
    {
        private readonly IFriendService _friendService;

        public FriendsController(IFriendService friendService) => _friendService = friendService;

        /// <summary> Wysyła zaproszenie do znajomych po nazwie użytkownika. </summary>
        [HttpPost("add-by-username/{username}")]
        public async Task<IActionResult> AddFriend(string username)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _friendService.SendFriendRequestAsync(userId, username);
            return HandleServiceResult(result);
        }

        /// <summary> Pobiera listę zaakceptowanych znajomych. </summary>
        [HttpGet("my-friends")]
        public async Task<IActionResult> GetMyFriends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _friendService.GetFriendsAsync(userId!));
        }

        /// <summary>
        /// Wysyła e-mail z zaproszeniem do rejestracji na podany adres.
        /// </summary>
        /// <param name="email">Adres e-mail zapraszanego znajomego.</param>
        [HttpPost("send-invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendInvite([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(ServiceResult.Failure("Email jest wymagany."));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _friendService.SendInviteEmailAsync(userId!, email);

            return HandleServiceResult(result);
        }

        /// <summary> Przeszukuje bazę zarejestrowanych użytkowników. </summary>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] DataTableRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _friendService.SearchUsersAsync(userId!, request));
        }

        /// <summary> Pobiera listę otrzymanych zaproszeń oczekujących na decyzję. </summary>
        [HttpGet("pending-requests")]
        public async Task<IActionResult> GetRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _friendService.GetIncomingRequestsAsync(userId!));
        }

        /// <summary> Akceptuje zaproszenie do znajomych. </summary>
        [HttpPost("accept/{requesterId}")]
        public async Task<IActionResult> Accept(string requesterId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _friendService.AcceptFriendRequestAsync(userId!, requesterId));
        }

        /// <summary> Odrzuca zaproszenie do znajomych </summary>
        [HttpDelete("reject-or-remove/{friendId}")]
        public async Task<IActionResult> Reject(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return HandleServiceResult(await _friendService.RejectFriendRequestAsync(userId!, friendId));
        }
    }
}
