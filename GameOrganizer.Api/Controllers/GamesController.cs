using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameOrganizer.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/games")]
    [Produces("application/json")]
    public class GamesController : GameOrganizerBaseController
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService) => _gameService = gameService;

        /// <summary>
        /// Tworzy nową grę i przypisuje ją do zalogowanego użytkownika.
        /// </summary>
        /// <param name="dto">Dane gry wraz z opcjonalnym plikiem okładki.</param>
        /// <returns>Zwraca utworzony obiekt gry.</returns>
        [HttpPost("create-game")]
        [Consumes("multipart/form-data")] 
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromForm] GameDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var game = await _gameService.AddGameAsync(dto, userId);
            return Ok(game);
        }

        /// <summary>
        /// Pobiera listę gier należących do zalogowanego użytkownika.
        /// </summary>
        /// <returns>Lista gier.</returns>
        [HttpGet("get-my-games")]
        [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyGames()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var games = await _gameService.GetMyGamesAsync(userId);
            return Ok(games);
        }

        /// <summary>
        /// Pobiera listę wszystkich dostępnych gatunków gier zdefiniowanych w systemie.
        /// </summary>
        /// <returns>Lista gatunków.</returns>
        [HttpGet("genres")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _gameService.GetAllGenresAsync();
            return Ok(genres);
        }
    }
}