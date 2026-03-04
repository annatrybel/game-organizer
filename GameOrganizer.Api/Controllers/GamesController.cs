using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameOrganizer.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class GamesController : GameOrganizerBaseController
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService) => _gameService = gameService;

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] GameDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var game = await _gameService.AddGameAsync(dto, userId);
            return Ok(game);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyGames()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var games = await _gameService.GetMyGamesAsync(userId);
            return Ok(games);
        }

        [HttpGet("genres")]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _gameService.GetAllGenresAsync();
            return Ok(genres);
        }
    }
}
