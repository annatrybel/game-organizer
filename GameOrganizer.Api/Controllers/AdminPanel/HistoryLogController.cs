using GameOrganizer.Api.Controllers;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameOrganizer.Api.Controllers.AdminPanel
{
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [Route("api/historyLog")]
    [Produces("application/json")]
    public class HistoryLogController : GameOrganizerBaseController
    {
        private readonly IHistoryLogService _historyLogService;

        public HistoryLogController(IHistoryLogService historyLogService)
        {
            _historyLogService = historyLogService;
        }

        /// <summary>
        /// Pobiera historię zmian (logi audytowe) w systemie.
        /// </summary>
        /// <remarks>
        /// Endpoint przeznaczony dla tabel danych (DataTable) z obsługą paginacji, sortowania i filtrowania po stronie serwera.
        /// Wymaga uprawnień Administratora.
        /// </remarks>
        /// <param name="request">Obiekt z parametrami paginacji, sortowania i wyszukiwania (DataTables).</param>
        /// <returns>Obiekt zawierający listę logów oraz metadane o całkowitej liczbie rekordów.</returns>
        [HttpPost("get-history-logs")]
        [ProducesResponseType(typeof(DataTableResponse<HistoryLogDto>), StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] 
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetHistoryLogs([FromBody] DataTableRequest request)
        {
            var serviceResponse = await _historyLogService.GetHistoryLogs(request);
            return HandleServiceResult(serviceResponse);
        }
    }
}
