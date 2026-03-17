using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Mvc;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IGameService
    {
        Task<ServiceResult<Game>> AddGameAsync(GameDto dto);
        Task<ServiceResult<DataTableResponse<Game>>> GetMyGamesAsync(string userId, DataTableRequest request);
        Task<ServiceResult<Game>> UpdateGameAsync(GameDto dto);
        Task<ServiceResult> DeleteGameAsync(int gameId);
        Task<ServiceResult> AddToUserCollectionAsync(int gameId, string userId);
        Task<ServiceResult<Game>> ProposeNewGameAsync(GameDto dto, string userId);
        Task<ServiceResult> AcceptGameAsync(int gameId);
        Task<ServiceResult> RejectGameAsync(int gameId, string? reason);
        Task<ServiceResult<DataTableResponse<Game>>> GetAvailableGamesAsync(DataTableRequest request);
        Task<ServiceResult<IEnumerable<Game>>> GetPendingAsync();
        Task<ServiceResult<IEnumerable<Genre>>> GetAllGenresAsync();
        Task<ServiceResult> RemoveFromCollectionAsync(int gameId, string userId);
        Task<ServiceResult<IEnumerable<Platform>>> GetAllPlatformsAsync();
    }
}
