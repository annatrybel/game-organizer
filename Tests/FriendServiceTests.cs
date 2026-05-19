using GameOrganizer.Api.Hubs;
using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
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
public class FriendServiceTests
{
    private GameOrganizerDbContext _dbContext = null!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<IHubContext<NotificationHub>> _hubContextMock = null!;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IClientProxy> _clientProxyMock = null!;
    private FriendService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GameOrganizerDbContext(options);

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(x => x.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var config = new Mock<IConfiguration>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<FriendService>>();
        var env = new Mock<IWebHostEnvironment>();

        _service = new FriendService(
            _dbContext,
            _userManagerMock.Object,
            _hubContextMock.Object,
            config.Object,
            emailSender.Object,
            logger.Object,
            env.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    private static ApplicationUser User(string id, string userName, string email)
        => new() { Id = id, UserName = userName, Email = email };

    [Test]
    public async Task SendFriendRequestAsync_WhenTargetNotFound_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.SendFriendRequestAsync("req-1", "missing");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Użytkownik.NotFound"));
    }

    [Test]
    public async Task SendFriendRequestAsync_WhenTargetIsSelf_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("me")).ReturnsAsync(User("req-1", "me", "me@test.com"));

        var result = await _service.SendFriendRequestAsync("req-1", "me");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Friends.Self"));
    }

    [Test]
    public async Task SendFriendRequestAsync_WhenRequesterMissing_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("target")).ReturnsAsync(User("target-1", "target", "t@test.com"));
        _userManagerMock.Setup(x => x.FindByIdAsync("req-1")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.SendFriendRequestAsync("req-1", "target");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Nadawca.NotFound"));
    }

    [Test]
    public async Task SendFriendRequestAsync_WhenRelationshipExists_ReturnsFailure()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("target")).ReturnsAsync(User("target-1", "target", "t@test.com"));
        _userManagerMock.Setup(x => x.FindByIdAsync("req-1")).ReturnsAsync(User("req-1", "req", "r@test.com"));

        _dbContext.Friendship.Add(new Friendship { RequesterId = "req-1", ReceiverId = "target-1", Status = FriendshipStatus.Pending });
        await _dbContext.SaveChangesAsync();

        var result = await _service.SendFriendRequestAsync("req-1", "target");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Friends.AlreadyExists"));
    }

    [Test]
    public async Task SendFriendRequestAsync_WhenValid_CreatesFriendshipAndNotification()
    {
        _userManagerMock.Setup(x => x.FindByNameAsync("target")).ReturnsAsync(User("target-1", "target", "t@test.com"));
        _userManagerMock.Setup(x => x.FindByIdAsync("req-1")).ReturnsAsync(User("req-1", "req", "r@test.com"));

        var result = await _service.SendFriendRequestAsync("req-1", "target");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await _dbContext.Friendship.CountAsync(), Is.EqualTo(1));
        Assert.That(await _dbContext.Notifications.CountAsync(), Is.EqualTo(1));
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AcceptFriendRequestAsync_WhenMissing_ReturnsFailure()
    {
        var result = await _service.AcceptFriendRequestAsync("current-1", "requester-1");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Zaproszenie.NotFound"));
    }

    [Test]
    public async Task AcceptFriendRequestAsync_WhenPending_UpdatesStatusAndSendsNotification()
    {
        _dbContext.Friendship.Add(new Friendship { RequesterId = "requester-1", ReceiverId = "current-1", Status = FriendshipStatus.Pending });
        await _dbContext.SaveChangesAsync();
        _userManagerMock.Setup(x => x.FindByIdAsync("current-1")).ReturnsAsync(User("current-1", "receiver", "c@test.com"));

        var result = await _service.AcceptFriendRequestAsync("current-1", "requester-1");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That((await _dbContext.Friendship.FirstAsync()).Status, Is.EqualTo(FriendshipStatus.Accepted));
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RejectFriendRequestAsync_WhenPending_UpdatesStatus()
    {
        _dbContext.Friendship.Add(new Friendship { RequesterId = "requester-1", ReceiverId = "current-1", Status = FriendshipStatus.Pending });
        await _dbContext.SaveChangesAsync();

        var result = await _service.RejectFriendRequestAsync("current-1", "requester-1");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That((await _dbContext.Friendship.FirstAsync()).Status, Is.EqualTo(FriendshipStatus.Rejected));
    }

    [Test]
    public async Task GetIncomingRequestsAsync_ReturnsOnlyPendingForReceiver()
    {
        var requester = User("requester-1", "requester", "r@test.com");
        var other = User("other-1", "other", "o@test.com");
        _dbContext.Users.AddRange(requester, other);
        _dbContext.Friendship.AddRange(
            new Friendship { RequesterId = requester.Id, ReceiverId = "current-1", Status = FriendshipStatus.Pending, Requester = requester },
            new Friendship { RequesterId = other.Id, ReceiverId = "current-1", Status = FriendshipStatus.Accepted, Requester = other }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetIncomingRequestsAsync("current-1");

        Assert.That(result.IsSuccess, Is.True);
        var list = result.Data.ToList();
        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0].UserId, Is.EqualTo("requester-1"));
        Assert.That(list[0].Status, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task GetFriendCollectionsWithGamesAsync_WhenNotFriends_ReturnsForbidden()
    {
        var result = await _service.GetFriendCollectionsWithGamesAsync("me", "friend");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.Forbidden"));
    }

    [Test]
    public async Task GetFriendCollectionsWithGamesAsync_WhenFriends_ReturnsPublicCollectionsOnly()
    {
        var genre = new Genre { Name = "RPG" };
        var platform = new Platform { Name = "PC" };
        var game = new Game { Title = "Game 1", Genre = genre, Platform = platform, Status = GameStatus.Accepted };
        _dbContext.Genres.Add(genre);
        _dbContext.Platforms.Add(platform);
        _dbContext.Games.Add(game);

        _dbContext.Friendship.Add(new Friendship { RequesterId = "me", ReceiverId = "friend", Status = FriendshipStatus.Accepted });

        var publicCollection = new Collection { Name = "Public", UserId = "friend", IsPublic = true };
        var privateCollection = new Collection { Name = "Private", UserId = "friend", IsPublic = false };
        _dbContext.Collections.AddRange(publicCollection, privateCollection);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserGames.AddRange(
            new UserGame { UserId = "friend", CollectionId = publicCollection.Id, GameId = game.Id },
            new UserGame { UserId = "friend", CollectionId = privateCollection.Id, GameId = game.Id }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetFriendCollectionsWithGamesAsync("me", "friend");

        Assert.That(result.IsSuccess, Is.True);
        var collections = result.Data.ToList();
        Assert.That(collections.Count, Is.EqualTo(1));
        Assert.That(collections[0].CollectionName, Is.EqualTo("Public"));
    }

    [Test]
    public async Task CompareGamesWithFriendAsync_WhenNotFriends_ReturnsForbidden()
    {
        var result = await _service.CompareGamesWithFriendAsync("me", "friend");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.Forbidden"));
    }

    [Test]
    public async Task CompareGamesWithFriendAsync_WhenFriends_ReturnsOwnershipComparison()
    {
        var genre = new Genre { Name = "RPG" };
        var platform = new Platform { Name = "PC" };
        var game1 = new Game { Title = "Shared", Genre = genre, Platform = platform, Status = GameStatus.Accepted };
        var game2 = new Game { Title = "Mine", Genre = genre, Platform = platform, Status = GameStatus.Accepted };
        _dbContext.Genres.Add(genre);
        _dbContext.Platforms.Add(platform);
        _dbContext.Games.AddRange(game1, game2);

        _dbContext.Friendship.Add(new Friendship { RequesterId = "me", ReceiverId = "friend", Status = FriendshipStatus.Accepted });

        var myCollection = new Collection { Name = "My", UserId = "me", IsPublic = true };
        var friendCollection = new Collection { Name = "Friend", UserId = "friend", IsPublic = true };
        _dbContext.Collections.AddRange(myCollection, friendCollection);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserGames.AddRange(
            new UserGame { UserId = "me", CollectionId = myCollection.Id, GameId = game1.Id },
            new UserGame { UserId = "friend", CollectionId = friendCollection.Id, GameId = game1.Id },
            new UserGame { UserId = "me", CollectionId = myCollection.Id, GameId = game2.Id }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _service.CompareGamesWithFriendAsync("me", "friend");

        Assert.That(result.IsSuccess, Is.True);
        var shared = result.Data.Single(x => x.Title == "Shared");
        Assert.That(shared.OwnedByMe, Is.True);
        Assert.That(shared.OwnedByFriend, Is.True);

        var mine = result.Data.Single(x => x.Title == "Mine");
        Assert.That(mine.OwnedByMe, Is.True);
        Assert.That(mine.OwnedByFriend, Is.False);
    }
}
