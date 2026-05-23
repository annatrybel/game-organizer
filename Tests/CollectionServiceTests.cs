using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto.Collections;
using GameOrganizer.Api.Services;
using GameOrganizer.Api.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

[TestFixture]
public class CollectionServiceTests
{
    private GameOrganizerDbContext _dbContext = null!;
    private CollectionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GameOrganizerDbContext(options);

        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var configuration = new Mock<IConfiguration>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<FriendService>>();
        var hostingEnvironment = new Mock<IWebHostEnvironment>();

        _service = new CollectionService(
            _dbContext,
            userManager.Object,
            hubContext.Object,
            configuration.Object,
            emailSender.Object,
            logger.Object,
            hostingEnvironment.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task InitDefaultCollectionsAsync_WhenUserAlreadyHasCollections_DoesNotDuplicate()
    {
        const string userId = "user-1";
        _dbContext.Collections.Add(new Collection { Name = "Existing", UserId = userId, IsPublic = true });
        await _dbContext.SaveChangesAsync();

        var result = await _service.InitDefaultCollectionsAsync(userId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await _dbContext.Collections.CountAsync(c => c.UserId == userId), Is.EqualTo(1));
    }

    [Test]
    public async Task InitDefaultCollectionsAsync_WhenNoCollections_CreatesSixDefaults()
    {
        const string userId = "user-2";

        var result = await _service.InitDefaultCollectionsAsync(userId);

        Assert.That(result.IsSuccess, Is.True);
        var created = await _dbContext.Collections.Where(c => c.UserId == userId).ToListAsync();
        Assert.That(created.Count, Is.EqualTo(6));
        Assert.That(created.All(c => c.IsPublic), Is.True);
    }

    [Test]
    public async Task CreateCollectionAsync_CreatesCollectionForUser()
    {
        var dto = new CollectionDto { Name = "My List", IsPublic = false };

        var result = await _service.CreateCollectionAsync(dto, "owner-1");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.Name, Is.EqualTo("My List"));
        Assert.That(result.Data.UserId, Is.EqualTo("owner-1"));
        Assert.That(result.Data.ShareCode, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task UpdateCollectionAsync_WhenCollectionNotFound_ReturnsFailure()
    {
        var dto = new CollectionDto { Id = 123, Name = "Changed", IsPublic = true };

        var result = await _service.UpdateCollectionAsync(dto, "user-1");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Collection.NotFound"));
    }

    [Test]
    public async Task UpdateCollectionAsync_WhenOwnedByUser_UpdatesFields()
    {
        var collection = new Collection { Name = "Old", IsPublic = false, UserId = "user-1" };
        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync();

        var dto = new CollectionDto { Id = collection.Id, Name = "New", IsPublic = true };
        var result = await _service.UpdateCollectionAsync(dto, "user-1");

        Assert.That(result.IsSuccess, Is.True);
        var updated = await _dbContext.Collections.FindAsync(collection.Id);
        Assert.That(updated!.Name, Is.EqualTo("New"));
        Assert.That(updated.IsPublic, Is.True);
    }

    [Test]
    public async Task DeleteCollectionAsync_WhenNotFound_ReturnsFailure()
    {
        var result = await _service.DeleteCollectionAsync(999, "user-1");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Collection.NotFound"));
    }

    [Test]
    public async Task DeleteCollectionAsync_WhenExistsAndOwned_DeletesCollection()
    {
        var collection = new Collection { Name = "ToDelete", UserId = "user-1" };
        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync();

        var result = await _service.DeleteCollectionAsync(collection.Id, "user-1");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await _dbContext.Collections.FindAsync(collection.Id), Is.Null);
    }

    [Test]
    public async Task GetUserCollectionsLookupAsync_ReturnsOnlyUserCollectionsOrderedByName()
    {
        _dbContext.Collections.AddRange(
            new Collection { Name = "Zeta", UserId = "user-1", IsPublic = true },
            new Collection { Name = "Alpha", UserId = "user-1", IsPublic = false },
            new Collection { Name = "Other", UserId = "user-2", IsPublic = true }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetUserCollectionsLookupAsync("user-1");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data[0].Name, Is.EqualTo("Alpha"));
        Assert.That(result.Data[1].Name, Is.EqualTo("Zeta"));
    }

    [Test]
    public async Task GetSharedCollectionAsync_WhenPrivateOrMissing_ReturnsFailure()
    {
        var collection = new Collection
        {
            Name = "Private",
            UserId = "owner-1",
            IsPublic = false,
            ShareCode = Guid.NewGuid()
        };
        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetSharedCollectionAsync(collection.ShareCode);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Collection.NotShared"));
    }

    [Test]
    public async Task GetSharedCollectionAsync_WhenPublic_ReturnsOwnerAndOrderedGames()
    {
        var owner = new ApplicationUser { Id = "owner-1", UserName = "owner" };
        var genre = new Genre { Name = "RPG" };
        var platform = new Platform { Name = "PC" };
        _dbContext.Users.Add(owner);
        _dbContext.Genres.Add(genre);
        _dbContext.Platforms.Add(platform);
        await _dbContext.SaveChangesAsync();

        var collection = new Collection
        {
            Name = "Public",
            UserId = owner.Id,
            User = owner,
            IsPublic = true,
            ShareCode = Guid.NewGuid()
        };

        var gameB = new Game { Title = "B", GenreId = genre.Id, Genre = genre, PlatformId = platform.Id, Platform = platform, Status = GameStatus.Accepted };
        var gameA = new Game { Title = "A", GenreId = genre.Id, Genre = genre, PlatformId = platform.Id, Platform = platform, Status = GameStatus.Accepted };

        _dbContext.Collections.Add(collection);
        _dbContext.Games.AddRange(gameA, gameB);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserGames.AddRange(
            new UserGame { UserId = owner.Id, CollectionId = collection.Id, GameId = gameB.Id },
            new UserGame { UserId = owner.Id, CollectionId = collection.Id, GameId = gameA.Id }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetSharedCollectionAsync(collection.ShareCode);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.OwnerName, Is.EqualTo("owner"));
        Assert.That(result.Data.Games.Select(g => g.Title).ToList(), Is.EqualTo(new[] { "A", "B" }));
    }
}
