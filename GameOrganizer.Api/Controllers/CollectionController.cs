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
        /// Pobiera listę gier użytkownika. Pozwala na filtrowanie wszystkich gier lub tylko tych z wybranej kolekcji.
        /// </summary>
        /// <param name="request">Parametry tabeli (strona, sortowanie, szukanie).</param>
        /// <param name="collectionId">Opcjonalne ID kolekcji. Jeśli puste, zwraca wszystkie gry użytkownika.</param>
        [HttpPost("my-collection")]
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
    }
}