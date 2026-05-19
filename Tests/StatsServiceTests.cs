using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Models.Dto.GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Tests;

[TestFixture]
public class StatsServiceTests
{
    private GameOrganizerDbContext _dbContext = null!;
    private IMemoryCache _cache = null!;
    private StatsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GameOrganizerDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new StatsService(_dbContext, _cache);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _cache.Dispose();
    }

    [Test]
    public async Task GetMyLibraryStatsAsync_ReturnsAggregatedStats()
    {
        const string userId = "user-1";
        var genreRpg = new Genre { Name = "RPG" };
        var genreFps = new Genre { Name = "FPS" };
        var platformPc = new Platform { Name = "PC" };
        var platformPs = new Platform { Name = "PS" };

        var game1 = new Game { Title = "Game 1", Genre = genreRpg, Platform = platformPc, Status = GameStatus.Accepted };
        var game2 = new Game { Title = "Game 2", Genre = genreRpg, Platform = platformPs, Status = GameStatus.Accepted };
        var game3 = new Game { Title = "Game 3", Genre = genreFps, Platform = platformPc, Status = GameStatus.Accepted };

        var collectionA = new Collection { Name = "A", UserId = userId };
        var collectionB = new Collection { Name = "B", UserId = userId };

        _dbContext.Genres.AddRange(genreRpg, genreFps);
        _dbContext.Platforms.AddRange(platformPc, platformPs);
        _dbContext.Games.AddRange(game1, game2, game3);
        _dbContext.Collections.AddRange(collectionA, collectionB);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserGames.AddRange(
            new UserGame { UserId = userId, GameId = game1.Id, CollectionId = collectionA.Id, AddedAt = DateTime.UtcNow.AddDays(-5) },
            new UserGame { UserId = userId, GameId = game2.Id, CollectionId = collectionA.Id, AddedAt = DateTime.UtcNow.AddDays(-40) },
            new UserGame { UserId = userId, GameId = game3.Id, CollectionId = collectionB.Id, AddedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetMyLibraryStatsAsync(userId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.TotalGames, Is.EqualTo(3));
        Assert.That(result.Data.AddedRecentlyCount, Is.EqualTo(2));
        Assert.That(result.Data.GamesByGenre.First().Label, Is.EqualTo("RPG"));
        Assert.That(result.Data.GamesByCollection.Single(x => x.Label == "A").Value, Is.EqualTo(2));
        Assert.That(result.Data.GamesByCollection.Single(x => x.Label == "B").Value, Is.EqualTo(1));
    }

    [Test]
    public async Task GetGlobalStatsAsync_ReturnsComputedGlobalData()
    {
        var u1 = new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.com" };
        var u2 = new ApplicationUser { Id = "u2", UserName = "u2", Email = "u2@test.com" };
        _dbContext.Users.AddRange(u1, u2);

        var genre = new Genre { Name = "RPG" };
        var platform = new Platform { Name = "PC" };
        var accepted = new Game { Title = "Accepted", Genre = genre, Platform = platform, Status = GameStatus.Accepted };
        var pending = new Game { Title = "Pending", Genre = genre, Platform = platform, Status = GameStatus.Pending };
        _dbContext.Genres.Add(genre);
        _dbContext.Platforms.Add(platform);
        _dbContext.Games.AddRange(accepted, pending);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserGames.AddRange(
            new UserGame { UserId = "u1", GameId = accepted.Id, CollectionId = 1 },
            new UserGame { UserId = "u2", GameId = accepted.Id, CollectionId = 2 }
        );

        _dbContext.UserRating.AddRange(
            new UserRating { UserId = "u1", GameId = accepted.Id, Value = 8 },
            new UserRating { UserId = "u2", GameId = accepted.Id, Value = 10 }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetGlobalStatsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.TotalUsers, Is.EqualTo(2));
        Assert.That(result.Data.TotalGamesInLibrary, Is.EqualTo(1));
        Assert.That(result.Data.TotalUserGames, Is.EqualTo(2));
        Assert.That(result.Data.MostPopularGames.Any(x => x.Label == "Accepted"), Is.True);
        Assert.That(result.Data.HighestRatedGames.Any(x => x.Label == "Accepted"), Is.True);
        Assert.That(result.Data.PopularGenres.Any(x => x.Label == "RPG"), Is.True);
    }

    [Test]
    public async Task GetGlobalStatsAsync_WhenCached_ReturnsCachedValue()
    {
        var cached = new GlobalStatsDto
        {
            TotalUsers = 77,
            TotalGamesInLibrary = 55,
            TotalUserGames = 44,
            MostPopularGames = new List<StatItemDto> { new() { Label = "Cached", Value = 1 } }
        };
        _cache.Set("GlobalStats_Cache", cached, TimeSpan.FromMinutes(10));

        var result = await _service.GetGlobalStatsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.TotalUsers, Is.EqualTo(77));
        Assert.That(result.Data.MostPopularGames.Single().Label, Is.EqualTo("Cached"));
    }
}
