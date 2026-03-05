using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameOrganizer.Api.Services
{
    public class GameService : IGameService
    {
        private readonly GameOrganizerDbContext _context;
        private readonly IFileService _fileService;

        public GameService(GameOrganizerDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<Game> AddGameAsync(GameDto dto, string userId)
        {
            string? url = null;

            if (dto.Image != null)
            {
                url = await _fileService.UploadImageAsync(dto.Image);
            }

            var game = new Game
            {
                Title = dto.Title,
                Description = dto.Description,
                GenreId = dto.GenreId,
                UserId = userId,
                ImageUrl = url
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return game;
        }

        public async Task<IEnumerable<Genre>> GetAllGenresAsync()
        {
            return await _context.Genres.OrderBy(g => g.Name).ToListAsync();
        }

        public async Task<IEnumerable<Game>> GetMyGamesAsync(string userId)
        {
            return await _context.Games
                .Include(g => g.Genre)
                .Where(g => g.UserId == userId)
                .ToListAsync();
        }
    }
}
