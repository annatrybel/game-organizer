using CloudinaryDotNet.Actions;
using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace GameOrganizer.Api.Services
{
    public class GameService : IGameService
    {
        private readonly GameOrganizerDbContext _context;
        private readonly IFileService _fileService;
        private const string ObjectName = "Game";

        public GameService(GameOrganizerDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<ServiceResult> AddToUserCollectionAsync(int gameId, string userId)
        {
            var alreadyHas = await _context.UserGames.AnyAsync(ug => ug.GameId == gameId && ug.UserId == userId);
            if (alreadyHas) return ServiceResult.Failure(GameErrors.GameAlreadyInCollection());

            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists) ServiceResult.Failure(CommonErrors.NotFound(ObjectName, gameId));

            var userGame = new UserGame { GameId = gameId, UserId = userId };
            _context.UserGames.Add(userGame);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<Game>> ProposeNewGameAsync(GameDto dto, string userId)
        {
            string? url = dto.Image != null ? await _fileService.UploadImageAsync(dto.Image) : null;

            var game = new Game
            {
                Title = dto.Title,
                Description = dto.Description,
                GenreId = dto.GenreId,
                ImageUrl = url,
                IsAccepted = false,
                SuggestedByUserId = userId
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return ServiceResult<Game>.Success(game);
        }

        public async Task<ServiceResult> AcceptGameAsync(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) ServiceResult.Failure(CommonErrors.NotFound(ObjectName, gameId));

            game.IsAccepted = true;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IEnumerable<Game>>> GetAvailableGamesAsync()
        {
            var games = await _context.Games
                .Include(g => g.Genre)
                .Where(g => g.IsAccepted)
                .OrderBy(g => g.Title)
                .ToListAsync();

            return ServiceResult<IEnumerable<Game>>.Success(games);
        }

        public async Task<ServiceResult<IEnumerable<Game>>> GetPendingAsync()
        {
            var pending = await _context.Games
                .Include(g => g.Genre)
                .Where(g => !g.IsAccepted)
                .ToListAsync();
            return ServiceResult<IEnumerable<Game>>.Success(pending);
        }

        public async Task<ServiceResult<Game>> AddGameAsync(GameDto dto, string userId)
        {
            string? url = dto.Image != null ? await _fileService.UploadImageAsync(dto.Image) : null;

            var game = new Game
            {
                Title = dto.Title,
                Description = dto.Description,
                GenreId = dto.GenreId,
                SuggestedByUserId = userId,
                ImageUrl = url,
                IsAccepted = true
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return ServiceResult<Game>.Success(game);
        }

        public async Task<ServiceResult<IEnumerable<Game>>> GetMyGamesAsync(string userId)
        {
            var games = await _context.UserGames
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Game)
                .ThenInclude(g => g.Genre)
                .Select(ug => ug.Game)
                .ToListAsync();

            return ServiceResult<IEnumerable<Game>>.Success(games);
        }

        public async Task<ServiceResult<Game>> UpdateGameAsync(GameDto dto)
        {
            var game = await _context.Games.FindAsync(dto.Id);
            if (game == null) ServiceResult.Failure(CommonErrors.NotFound(ObjectName, dto.Id));

            game.Title = dto.Title;
            game.Description = dto.Description;
            game.GenreId = dto.GenreId;

            if (dto.Image != null)
            {
                if (!string.IsNullOrEmpty(game.ImageUrl) && game.ImageUrl.Contains("cloudinary.com"))
                {
                    await _fileService.DeleteImageAsync(game.ImageUrl);
                }
                game.ImageUrl = await _fileService.UploadImageAsync(dto.Image);
            }

            await _context.SaveChangesAsync();
            return ServiceResult<Game>.Success(game);
        }

        public async Task<ServiceResult> DeleteGameAsync(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, gameId));

            if (!string.IsNullOrEmpty(game.ImageUrl) && game.ImageUrl.Contains("cloudinary.com"))
            {
                await _fileService.DeleteImageAsync(game.ImageUrl);
            }

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IEnumerable<Genre>>> GetAllGenresAsync()
        {
            var genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return ServiceResult<IEnumerable<Genre>>.Success(genres);
        }
    }
}