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
    }
}
