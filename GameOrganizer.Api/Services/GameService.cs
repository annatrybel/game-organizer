using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

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

        public async Task<ServiceResult> AddToUserCollectionAsync(int gameId, int collectionId, string userId)
        {            

            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists) ServiceResult.Failure(CommonErrors.NotFound(ObjectName, gameId));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId);

            if (collection == null)
                return ServiceResult.Failure(CommonErrors.NotFound("Collection", collectionId));

            var alreadyHas = await _context.UserGames.AnyAsync(ug => ug.GameId == gameId && ug.UserId == userId);
            if (alreadyHas) return ServiceResult.Failure(GameErrors.GameAlreadyInCollection());

            var userGame = new UserGame
            {
                GameId = gameId,
                UserId = userId,
                CollectionId = collectionId
            };

            _context.UserGames.Add(userGame);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> MoveGameAsync(int gameId, int currentCollectionId, int targetCollectionId, string userId)
        {
            var userGame = await _context.UserGames
                .FirstOrDefaultAsync(ug => ug.GameId == gameId && ug.CollectionId == currentCollectionId && ug.UserId == userId);

            if (userGame == null) return ServiceResult.Failure(CommonErrors.NotFound("GameRecord", gameId));

            var targetExists = await _context.Collections.AnyAsync(c => c.Id == targetCollectionId && c.UserId == userId);
            if (!targetExists) return ServiceResult.Failure(CommonErrors.NotFound("TargetCollection", targetCollectionId));

            userGame.CollectionId = targetCollectionId;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<Game>> ProposeNewGameAsync(GameDto dto, string userId)
        {
            var existingGame = await _context.Games
                .FirstOrDefaultAsync(g => g.Title.ToLower() == dto.Title.ToLower());

            if (existingGame != null)
            {
                if (existingGame.Status == GameStatus.Accepted)
                {
                    return ServiceResult<Game>.Failure(GameErrors.GameAlreadyExists(dto.Title));
                }
                else
                {
                    return ServiceResult<Game>.Failure(GameErrors.ProposalAlreadyExists());
                }
            }

            var genreExists = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);
            if (!genreExists)
                return ServiceResult<Game>.Failure(CommonErrors.NotFound("Gatunek", dto.GenreId));

            if (!await _context.Platforms.AnyAsync(p => p.Id == dto.PlatformId)) 
                return ServiceResult<Game>.Failure(CommonErrors.NotFound("Platforma", dto.PlatformId));

            string? url = dto.Image != null ? await _fileService.UploadImageAsync(dto.Image) : null;

            var game = new Game
            {
                Title = dto.Title,
                Description = dto.Description,
                GenreId = dto.GenreId,
                PlatformId = dto.PlatformId,
                ImageUrl = url,
                Status = GameStatus.Pending,
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

            game.Status = GameStatus.Accepted;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }


        public async Task<ServiceResult> RejectGameAsync(int gameId, string? reason)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, gameId));

            if (game.Status == GameStatus.Accepted)
                return ServiceResult.Failure(new ServiceError("Game.Error", "Nie można odrzucić już zaakceptowanej gry."));

            game.Status = GameStatus.Rejected;
            game.RejectionReason = reason;

            //if (!string.IsNullOrEmpty(game.ImageUrl) && game.ImageUrl.Contains("cloudinary.com"))
            //{
            //    await _fileService.DeleteImageAsync(game.ImageUrl);
            //    game.ImageUrl = null;
            //}

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<DataTableResponse<Game>>> GetAvailableGamesAsync(DataTableRequest request)
        {
            try
            {
                string[] columnNames = { "Title", "Genre.Name", "Platform.Name", "Description" };

                string sortColumn = (request.OrderColumn >= 0 && request.OrderColumn < columnNames.Length)
                    ? columnNames[request.OrderColumn]
                    : "Title";

                var baseQuery = _context.Games
                    .Include(g => g.Genre)
                    .Include(g => g.Platform)
                    .Where(g => g.Status == GameStatus.Accepted)
                    .AsQueryable();

                var totalRecords = await baseQuery.CountAsync();
                if (!string.IsNullOrEmpty(request.SearchValue))
                {
                    string searchValueLower = request.SearchValue.ToLower();
                    baseQuery = baseQuery.Where(g =>
                        g.Title.ToLower().Contains(searchValueLower) ||
                        (g.Description != null && g.Description.ToLower().Contains(searchValueLower)) ||
                        g.Genre.Name.ToLower().Contains(searchValueLower) ||
                        g.Platform.Name.ToLower().Contains(searchValueLower)); 
                }

                var recordsFiltered = await baseQuery.CountAsync();

                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(request.OrderDir))
                {
                    baseQuery = baseQuery.OrderBy(sortColumn + " " + request.OrderDir);
                }

                var data = await baseQuery
                    .Skip(request.Start)
                    .Take(request.Length)
                    .ToListAsync();

                return ServiceResult<DataTableResponse<Game>>.Success(new DataTableResponse<Game>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<DataTableResponse<Game>>.Failure(CommonErrors.DataProcessingError());
            }
        }

        public async Task<ServiceResult<IEnumerable<Game>>> GetPendingAsync()
        {
            var pending = await _context.Games
                .Include(g => g.Genre)
                .Where(g => g.Status == GameStatus.Pending)
                .ToListAsync();
            return ServiceResult<IEnumerable<Game>>.Success(pending);
        }

        public async Task<ServiceResult<Game>> AddGameAsync(GameDto dto)
        {
            var titleExists = await _context.Games.AnyAsync(g => g.Title.ToLower() == dto.Title.ToLower());
            if (titleExists)
                return ServiceResult<Game>.Failure(GameErrors.GameAlreadyExists(dto.Title));

            var genreExists = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);
            if (!genreExists)
                return ServiceResult<Game>.Failure(CommonErrors.NotFound("Gatunek", dto.GenreId));

            if (!await _context.Platforms.AnyAsync(p => p.Id == dto.PlatformId)) 
                return ServiceResult<Game>.Failure(CommonErrors.NotFound("Platforma", dto.PlatformId));

            string? url = dto.Image != null ? await _fileService.UploadImageAsync(dto.Image) : null;

            var game = new Game
            {
                Title = dto.Title,
                Description = dto.Description,
                GenreId = dto.GenreId,
                PlatformId = dto.PlatformId,
                ImageUrl = url,
                Status = GameStatus.Accepted
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return ServiceResult<Game>.Success(game);
        }

       
        public async Task<ServiceResult<Game>> UpdateGameAsync(GameDto dto)
        {
            var game = await _context.Games.FindAsync(dto.Id);
            if (game == null) ServiceResult.Failure(CommonErrors.NotFound(ObjectName, dto.Id));
                        
            if (!await _context.Genres.AnyAsync(g => g.Id == dto.GenreId))
                return ServiceResult<Game>.Failure(CommonErrors.NotFound("Gatunek", dto.GenreId));

            if (!await _context.Platforms.AnyAsync(p => p.Id == dto.PlatformId)) 
                return ServiceResult<Game>.Failure(CommonErrors.NotFound("Platforma", dto.PlatformId));

            game.Title = dto.Title;
            game.Description = dto.Description;
            game.GenreId = dto.GenreId;
            game.PlatformId = dto.PlatformId;

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

        public async Task<ServiceResult> RemoveFromCollectionAsync(int gameId, string userId)
        {
            var userGame = await _context.UserGames
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == gameId);

            if (userGame == null)
            {
                return ServiceResult.Failure(CommonErrors.NotFound("UserGame", gameId));
            }

            _context.UserGames.Remove(userGame);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        public async Task<ServiceResult<IEnumerable<Platform>>> GetAllPlatformsAsync()
        {
            var platforms = await _context.Platforms.OrderBy(p => p.Name).ToListAsync();
            return ServiceResult<IEnumerable<Platform>>.Success(platforms);
        }
    }
}