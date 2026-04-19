using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Models.Dto.GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameOrganizer.Api.Controllers
{
    [Authorize]
    [Route("api/statistics")]
    [ApiController]
    public class StatsController : GameOrganizerBaseController
    {
        private readonly IStatsService _statsService;

        public StatsController(IStatsService statsService)
        {
            _statsService = statsService;
        }

        /// <summary>
        /// Pobiera prywatne statystyki zalogowanego użytkownika.
        /// </summary>
        [HttpGet("my-library")]
        [ProducesResponseType(typeof(UserLibraryStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _statsService.GetMyLibraryStatsAsync(userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera globalne statystyki całego systemu.
        /// </summary>
        [HttpGet("global")]
        [ProducesResponseType(typeof(GlobalStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGlobalStats()
        {
            var result = await _statsService.GetGlobalStatsAsync();
            return HandleServiceResult(result);
        }
    }
}