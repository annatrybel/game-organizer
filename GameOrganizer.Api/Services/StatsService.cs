using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Models.Dto.GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameOrganizer.Api.Services
{
    public class StatsService : IStatsService
    {
        private readonly GameOrganizerDbContext _context;
        private readonly IMemoryCache _cache;
        private const string GlobalStatsCacheKey = "GlobalStats_Cache";

        public StatsService(GameOrganizerDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<ServiceResult<UserLibraryStatsDto>> GetMyLibraryStatsAsync(string userId)
        {
            try
            {
                var userGamesQuery = _context.UserGames.Where(ug => ug.UserId == userId);

                var totalGames = await userGamesQuery.CountAsync();
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var addedRecently = await userGamesQuery.CountAsync(ug => ug.AddedAt >= thirtyDaysAgo);

                var gamesByGenre = await userGamesQuery
                    .GroupBy(ug => ug.Game.Genre.Name)
                    .Select(g => new StatItemDto { Label = g.Key, Value = g.Count() })
                    .OrderByDescending(x => x.Value)
                    .Take(5)
                    .ToListAsync();

                var gamesByPlatform = await userGamesQuery
                    .GroupBy(ug => ug.Game.Platform.Name)
                    .Select(g => new StatItemDto { Label = g.Key, Value = g.Count() })
                    .OrderByDescending(x => x.Value)
                    .ToListAsync();

                var gamesByCollection = await _context.Collections
                    .Where(c => c.UserId == userId)
                    .Select(c => new StatItemDto
                    {
                        Label = c.Name,
                        Value = c.UserGames.Count
                    })
                    .OrderByDescending(x => x.Value)
                    .ToListAsync();

                var stats = new UserLibraryStatsDto
                {
                    TotalGames = totalGames,
                    AddedRecentlyCount = addedRecently,
                    GamesByGenre = gamesByGenre,
                    GamesByPlatform = gamesByPlatform,
                    GamesByCollection = gamesByCollection
                };

                return ServiceResult<UserLibraryStatsDto>.Success(stats);
            }
            catch (Exception)
            {
                return ServiceResult<UserLibraryStatsDto>.Failure(CommonErrors.DataProcessingError());
            }
        }

        public async Task<ServiceResult<GlobalStatsDto>> GetGlobalStatsAsync()
        {
            if (_cache.TryGetValue(GlobalStatsCacheKey, out GlobalStatsDto cachedStats))
            {
                return ServiceResult<GlobalStatsDto>.Success(cachedStats);
            }

            try
            {
                var counts = await _context.Users
                    .Select(_ => new
                    {
                        Users = _context.Users.Count(),
                        Library = _context.Games.Count(g => g.Status == GameStatus.Accepted),
                        UserGames = _context.UserGames.Count()
                    })
                    .FirstOrDefaultAsync() ?? new { Users = 0, Library = 0, UserGames = 0 };

                var mostPopularGames = await _context.UserGames
                    .GroupBy(ug => ug.Game.Title)
                    .Select(g => new StatItemDto { Label = g.Key, Value = g.Count() })
                    .OrderByDescending(x => x.Value)
                    .Take(10).ToListAsync();

                var highestRatedGames = await _context.UserRating
                    .GroupBy(r => r.Game.Title)
                    .Select(g => new
                    {
                        Title = g.Key,
                        Average = g.Average(r => (double)r.Value),
                        Count = g.Count()
                    })
                    .Where(x => x.Count >= 1)
                    .OrderByDescending(x => x.Average)
                    .Take(10)
                    .Select(x => new StatItemDto { Label = x.Title, Value = (int)Math.Round(x.Average * 10) })
                    .ToListAsync();

                var popularGenres = await _context.UserGames
                    .GroupBy(ug => ug.Game.Genre.Name)
                    .Select(g => new StatItemDto { Label = g.Key, Value = g.Count() })
                    .OrderByDescending(x => x.Value)
                    .Take(5).ToListAsync();

                var popularPlatforms = await _context.UserGames
                    .GroupBy(ug => ug.Game.Platform.Name)
                    .Select(g => new StatItemDto { Label = g.Key, Value = g.Count() })
                    .OrderByDescending(x => x.Value).ToListAsync();

                var stats = new GlobalStatsDto
                {
                    TotalUsers = counts.Users,
                    TotalGamesInLibrary = counts.Library,
                    TotalUserGames = counts.UserGames,
                    MostPopularGames = mostPopularGames,
                    HighestRatedGames = highestRatedGames,
                    PopularGenres = popularGenres,
                    PopularPlatforms = popularPlatforms
                };

                _cache.Set(GlobalStatsCacheKey, stats, TimeSpan.FromMinutes(10));

                return ServiceResult<GlobalStatsDto>.Success(stats);
            }
            catch (Exception)
            {
                return ServiceResult<GlobalStatsDto>.Failure(CommonErrors.DataProcessingError());
            }
        }
    }
}