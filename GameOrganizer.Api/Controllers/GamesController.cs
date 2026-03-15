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
        /// Dodaje nową grę bezpośrednio do ogólnodostępnej biblioteki gier.
        /// </summary>
        /// <remarks>
        /// Metoda dostępna wyłącznie dla administratorów. Gra jest automatycznie zatwierdzana i staje się widoczna dla wszystkich użytkowników.
        /// </remarks>
        /// <param name="dto">Dane gry wraz z opcjonalnym plikiem obrazu okładki.</param>
        /// <returns>Obiekt nowo utworzonej gry.</returns>v
        [Authorize(Roles = "Administrator")]
        [HttpPost("create-game")]
        [Consumes("multipart/form-data")] 
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromForm] GameDto dto)
        {
            var result = await _gameService.AddGameAsync(dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera listę gier dostępnych w globalnej bibliotece (DataTable - stronicowanie, wyszukiwanie, sortowanie).
        /// </summary>
        /// <param name="request">Parametry zapytania DataTable.</param>
        /// <returns>Strona danych dla tabeli.</returns>
        [HttpPost("available-table")]
        [ProducesResponseType(typeof(DataTableResponse<Game>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableTable([FromBody] DataTableRequest request)
        {
            var result = await _gameService.GetAvailableGamesAsync(request);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Przypisuje istniejącą grę z biblioteki do prywatnej kolekcji zalogowanego użytkownika.
        /// </summary>
        /// <param name="gameId">Unikalny identyfikator gry.</param>
        /// <returns>Status operacji dodawania.</returns>
        [HttpPost("add-to-collection/{gameId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddToCollection(int gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _gameService.AddToUserCollectionAsync(gameId, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Przesyła propozycję nowej gry do bazy danych, która wymaga akceptacji przez administratora.
        /// </summary>
        /// <param name="dto">Dane proponowanej gry.</param>
        /// <returns>Obiekt gry ze statusem oczekującym na akceptację.</returns>
        [HttpPost("propose")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Propose([FromForm] GameDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _gameService.ProposeNewGameAsync(dto, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera listę gier znajdujących się w kolekcji użytkownika (DataTable - stronicowanie, wyszukiwanie, sortowanie).
        /// </summary>
        /// <param name="request">Parametry zapytania DataTable.</param>
        /// <returns>Strona gier użytkownika.</returns>
        [HttpPost("my-collection")]
        [ProducesResponseType(typeof(DataTableResponse<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyGames([FromBody] DataTableRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Obsługa braku ID użytkownika w claimach
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _gameService.GetMyGamesAsync(userId, request);
            return HandleServiceResult(result);
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
            var result = await _gameService.GetAllGenresAsync();
            return HandleServiceResult(result);
        }
        /// <summary>
        /// Aktualizuje dane istniejącej gry w głównej bibliotece (dostępne tylko dla administratorów).
        /// </summary>
        /// <param name="dto">Zaktualizowane dane gry.</param>
        /// <returns>Zaktualizowany obiekt gry.</returns>
        [Authorize(Roles = "Administrator")]
        [HttpPut("update-game")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromForm] GameDto dto)
        {
            var result = await _gameService.UpdateGameAsync(dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Usuwa grę z globalnej biblioteki oraz ze wszystkich kolekcji użytkowników (dostępne tylko dla administratorów).
        /// </summary>
        /// <param name="id">ID gry do usunięcia.</param>
        /// <returns>Status operacji.</returns>
        [Authorize(Roles = "Administrator")]
        [HttpDelete("delete-game/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _gameService.DeleteGameAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera listę gier zaproponowanych przez użytkowników, które oczekują na zatwierdzenie (dostępne tylko dla administratorów).
        /// </summary>
        /// <returns>Lista gier o statusie IsAccepted = false.</returns>
        [Authorize(Roles = "Administrator")]
        [HttpGet("pending-approvals")]
        [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPending()
        {
            var result = await _gameService.GetPendingAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Akceptuje propozycję gry, czyniąc ją widoczną dla wszystkich użytkowników (dostępne tylko dla administratorów).
        /// </summary>
        /// <param name="id">ID gry do zatwierdzenia.</param>
        /// <returns>Status operacji zatwierdzenia.</returns>
        [Authorize(Roles = "Administrator")]
        [HttpPost("accept/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptGame(int id)
        {
            var result = await _gameService.AcceptGameAsync(id);
            return HandleServiceResult(result);
        }
    }
}