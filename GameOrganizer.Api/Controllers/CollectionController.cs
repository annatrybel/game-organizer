using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameOrganizer.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/collections")]
    [Produces("application/json")]
    public class CollectionController : GameOrganizerBaseController
    {
        private readonly ICollectionService _collectionService;

        public CollectionController(ICollectionService collectionService) => _collectionService = collectionService;

        /// <summary>
        /// Pobiera listę nazw kolekcji użytkownika
        /// </summary>
        [HttpGet("lookup")]
        [ProducesResponseType(typeof(List<CollectionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCollectionsLookup()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _collectionService.GetUserCollectionsLookupAsync(userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera kolekcje wraz z grami w formacie paginowanym (DataTables)
        /// </summary>
        /// <param name="request">Parametry tabeli (strona, sortowanie, szukanie).</param>
        /// <param name="collectionId">Opcjonalne ID kolekcji. Jeśli puste, zwraca wszystkie gry użytkownika.</param>
        [HttpPost("grouped-with-games")]
        [ProducesResponseType(typeof(DataTableResponse<CollectionWithGamesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyGames([FromBody] DataTableRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _collectionService.GetMyGamesAsync(userId, request);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Tworzy nową, niestandardową kolekcję gier.
        /// </summary>
        /// <param name="dto">Dane kolekcji (nazwa i ustawienia prywatności).</param>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Collection), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CollectionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _collectionService.CreateCollectionAsync(dto, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Aktualizuje nazwę lub ustawienia prywatności istniejącej kolekcji.
        /// </summary>
        /// <param name="dto">Dane do aktualizacji.</param>
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromBody] CollectionDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _collectionService.UpdateCollectionAsync(dto, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Usuwa kolekcję użytkownika oraz przypisae gry do tej kolekcji.
        /// </summary>
        /// <param name="id">ID kolekcji do usunięcia.</param>
        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _collectionService.DeleteCollectionAsync(id, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera zawartość kolekcji na podstawie publicznego kodu udostępniania.
        /// Dostępne dla wszystkich.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("share/{shareCode}")]
        [ProducesResponseType(typeof(SharedCollectionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSharedCollection(Guid shareCode)
        {
            var result = await _collectionService.GetSharedCollectionAsync(shareCode);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Udostępnia publiczną kolekcję gier innemu użytkownikowi za pomocą wiadomości e-mail.
        /// </summary>
        /// <remarks>
        /// Metoda generuje unikalny link do kolekcji na podstawie jej kodu ShareCode i wysyła go na podany adres.
        /// Kolekcja musi mieć status "Publiczna", aby wysłanie było możliwe.
        /// </remarks>
        /// <param name="id">Unikalny identyfikator (ID) kolekcji, którą chcesz udostępnić.</param>
        /// <param name="recipientEmail">Adres e-mail osoby, do której ma trafić zaproszenie.</param>
        /// <returns>Zwraca status 200 OK przy powodzeniu lub 400 Bad Request, jeśli kolekcja jest prywatna.</returns>
        /// <response code="200">Wiadomość e-mail została pomyślnie wysłana.</response>
        /// <response code="400">Błąd walidacji (np. próba udostępnienia kolekcji prywatnej).</response>
        /// <response code="401">Użytkownik nie jest zalogowany.</response>
        /// <response code="404">Kolekcja o podanym ID nie istnieje lub nie należy do zalogowanego użytkownika.</response>
        [HttpPost("{id}/share-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ShareCollectionByEmail(int id, [FromQuery] string recipientEmail)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _collectionService.ShareCollectionByEmailAsync(id, userId, recipientEmail);
            return HandleServiceResult(result);
        }
    }
}