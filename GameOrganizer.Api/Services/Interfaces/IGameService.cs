using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IGameService
    {
        Task<Game> AddGameAsync(GameDto dto, string userId);
        Task<IEnumerable<Game>> GetMyGamesAsync(string userId);
        Task<Game?> UpdateGameAsync(GameDto dto, string userId);
        Task<bool> DeleteGameAsync(int gameId, string userId);

        Task<IEnumerable<Genre>> GetAllGenresAsync();
    }
}
