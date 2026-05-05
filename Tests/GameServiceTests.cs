using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests
{
    [TestFixture]
    public class GameServiceTests
    {
        private GameOrganizerDbContext _dbContext = null!;
        private Mock<IFileService> _fileServiceMock = null!;
        private GameService _gameService = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new GameOrganizerDbContext(options);
            _fileServiceMock = new Mock<IFileService>();
            _gameService = new GameService(_dbContext, _fileServiceMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        private async Task<Genre> SeedGenreAsync(string name = "RPG")
        {
            var genre = new Genre { Name = name };
            _dbContext.Genres.Add(genre);
            await _dbContext.SaveChangesAsync();
            return genre;
        }

        private async Task<Platform> SeedPlatformAsync(string name = "PC")
        {
            var platform = new Platform { Name = name };
            _dbContext.Platforms.Add(platform);
            await _dbContext.SaveChangesAsync();
            return platform;
        }

        private async Task<Game> SeedGameAsync(string title = "Test Game", GameStatus status = GameStatus.Accepted, int? genreId = null, int? platformId = null)
        {
            var gId = genreId ?? (await SeedGenreAsync()).Id;
            var pId = platformId ?? (await SeedPlatformAsync()).Id;
            var game = new Game { Title = title, Status = status, GenreId = gId, PlatformId = pId };
            _dbContext.Games.Add(game);
            await _dbContext.SaveChangesAsync();
            return game;
        }

        private async Task<Collection> SeedCollectionAsync(string userId, string name = "Favorites")
        {
            var collection = new Collection { Name = name, UserId = userId };
            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();
            return collection;
        }

        #region AddGameAsync

        [Test]
        public async Task AddGameAsync_TitleAlreadyExists_ReturnsFailure()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            await SeedGameAsync("Existing Game", GameStatus.Accepted, genre.Id, platform.Id);
            var dto = new GameDto { Title = "Existing Game", GenreId = genre.Id, PlatformId = platform.Id };

            var result = await _gameService.AddGameAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.AlreadyExists"));
        }

        [Test]
        public async Task AddGameAsync_TitleComparison_IsCaseInsensitive()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            await SeedGameAsync("existing game", GameStatus.Accepted, genre.Id, platform.Id);
            var dto = new GameDto { Title = "EXISTING GAME", GenreId = genre.Id, PlatformId = platform.Id };

            var result = await _gameService.AddGameAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.AlreadyExists"));
        }

        [Test]
        public async Task AddGameAsync_GenreNotFound_ReturnsFailure()
        {
            var platform = await SeedPlatformAsync();
            var dto = new GameDto { Title = "New Game", GenreId = 9999, PlatformId = platform.Id };

            var result = await _gameService.AddGameAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Gatunek.NotFound"));
        }

        [Test]
        public async Task AddGameAsync_PlatformNotFound_ReturnsFailure()
        {
            var genre = await SeedGenreAsync();
            var dto = new GameDto { Title = "New Game", GenreId = genre.Id, PlatformId = 9999 };

            var result = await _gameService.AddGameAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Platforma.NotFound"));
        }

        [Test]
        public async Task AddGameAsync_ValidData_ReturnsSuccessWithAcceptedStatus()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            var dto = new GameDto { Title = "New Game", Description = "A great game", GenreId = genre.Id, PlatformId = platform.Id };

            var result = await _gameService.AddGameAsync(dto);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data.Title, Is.EqualTo("New Game"));
            Assert.That(result.Data.Status, Is.EqualTo(GameStatus.Accepted));
        }

        [Test]
        public async Task AddGameAsync_WithImage_UploadsImageAndSetsUrl()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            var imageMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            _fileServiceMock.Setup(x => x.UploadImageAsync(imageMock.Object))
                .ReturnsAsync("https://cloudinary.com/image.jpg");

            var dto = new GameDto { Title = "Game With Image", GenreId = genre.Id, PlatformId = platform.Id, Image = imageMock.Object };

            var result = await _gameService.AddGameAsync(dto);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data.ImageUrl, Is.EqualTo("https://cloudinary.com/image.jpg"));
        }

        #endregion

        #region DeleteGameAsync

        [Test]
        public async Task DeleteGameAsync_GameNotFound_ReturnsFailure()
        {
            var result = await _gameService.DeleteGameAsync(9999);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.NotFound"));
        }

        [Test]
        public async Task DeleteGameAsync_GameExists_ReturnsSuccess()
        {
            var game = await SeedGameAsync();

            var result = await _gameService.DeleteGameAsync(game.Id);

            Assert.That(result.IsSuccess, Is.True);
            var deletedGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.That(deletedGame, Is.Null);
        }

        [Test]
        public async Task DeleteGameAsync_GameWithCloudinaryImage_DeletesImage()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            var game = new Game { Title = "Game", Status = GameStatus.Accepted, GenreId = genre.Id, PlatformId = platform.Id, ImageUrl = "https://cloudinary.com/image.jpg" };
            _dbContext.Games.Add(game);
            await _dbContext.SaveChangesAsync();

            var result = await _gameService.DeleteGameAsync(game.Id);

            Assert.That(result.IsSuccess, Is.True);
            _fileServiceMock.Verify(x => x.DeleteImageAsync("https://cloudinary.com/image.jpg"), Times.Once);
        }

        #endregion

        #region RejectGameAsync

        [Test]
        public async Task RejectGameAsync_GameNotFound_ReturnsFailure()
        {
            var result = await _gameService.RejectGameAsync(9999, "No reason");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.NotFound"));
        }

        [Test]
        public async Task RejectGameAsync_AlreadyAccepted_ReturnsFailure()
        {
            var game = await SeedGameAsync(status: GameStatus.Accepted);

            var result = await _gameService.RejectGameAsync(game.Id, "Too late");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.Error"));
        }

        [Test]
        public async Task RejectGameAsync_PendingGame_SetsRejectedStatusAndReason()
        {
            var game = await SeedGameAsync(status: GameStatus.Pending);
            const string reason = "Duplicate entry";

            var result = await _gameService.RejectGameAsync(game.Id, reason);

            Assert.That(result.IsSuccess, Is.True);
            var updatedGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.That(updatedGame!.Status, Is.EqualTo(GameStatus.Rejected));
            Assert.That(updatedGame.RejectionReason, Is.EqualTo(reason));
        }

        #endregion

        #region AcceptGameAsync

        [Test]
        public async Task AcceptGameAsync_GameExists_SetsAcceptedStatus()
        {
            var game = await SeedGameAsync(status: GameStatus.Pending);

            var result = await _gameService.AcceptGameAsync(game.Id);

            Assert.That(result.IsSuccess, Is.True);
            var updatedGame = await _dbContext.Games.FindAsync(game.Id);
            Assert.That(updatedGame!.Status, Is.EqualTo(GameStatus.Accepted));
        }

        #endregion

        #region ProposeNewGameAsync

        [Test]
        public async Task ProposeNewGameAsync_GameAlreadyAccepted_ReturnsFailure()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            await SeedGameAsync("Already Accepted", GameStatus.Accepted, genre.Id, platform.Id);
            var dto = new GameDto { Title = "Already Accepted", GenreId = genre.Id, PlatformId = platform.Id };

            var result = await _gameService.ProposeNewGameAsync(dto, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.AlreadyExists"));
        }

        [Test]
        public async Task ProposeNewGameAsync_GameAlreadyPending_ReturnsFailure()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            await SeedGameAsync("Pending Proposal", GameStatus.Pending, genre.Id, platform.Id);
            var dto = new GameDto { Title = "Pending Proposal", GenreId = genre.Id, PlatformId = platform.Id };

            var result = await _gameService.ProposeNewGameAsync(dto, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.ProposalAlreadyExists"));
        }

        [Test]
        public async Task ProposeNewGameAsync_GenreNotFound_ReturnsFailure()
        {
            var platform = await SeedPlatformAsync();
            var dto = new GameDto { Title = "Brand New Game", GenreId = 9999, PlatformId = platform.Id };

            var result = await _gameService.ProposeNewGameAsync(dto, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Gatunek.NotFound"));
        }

        [Test]
        public async Task ProposeNewGameAsync_PlatformNotFound_ReturnsFailure()
        {
            var genre = await SeedGenreAsync();
            var dto = new GameDto { Title = "Brand New Game", GenreId = genre.Id, PlatformId = 9999 };

            var result = await _gameService.ProposeNewGameAsync(dto, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Platforma.NotFound"));
        }

        [Test]
        public async Task ProposeNewGameAsync_ValidData_ReturnsSuccessWithPendingStatus()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            var dto = new GameDto { Title = "Proposed Game", Description = "New proposal", GenreId = genre.Id, PlatformId = platform.Id };
            const string userId = "proposing-user";

            var result = await _gameService.ProposeNewGameAsync(dto, userId);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data.Status, Is.EqualTo(GameStatus.Pending));
            Assert.That(result.Data.SuggestedByUserId, Is.EqualTo(userId));
        }

        #endregion

        #region AddToUserCollectionAsync

        [Test]
        public async Task AddToUserCollectionAsync_CollectionNotFound_ReturnsFailure()
        {
            var game = await SeedGameAsync();

            var result = await _gameService.AddToUserCollectionAsync(game.Id, 9999, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Collection.NotFound"));
        }

        [Test]
        public async Task AddToUserCollectionAsync_CollectionBelongsToDifferentUser_ReturnsFailure()
        {
            var game = await SeedGameAsync();
            var collection = await SeedCollectionAsync("other-user");

            var result = await _gameService.AddToUserCollectionAsync(game.Id, collection.Id, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Collection.NotFound"));
        }

        [Test]
        public async Task AddToUserCollectionAsync_GameAlreadyInCollection_ReturnsFailure()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync();
            var collection = await SeedCollectionAsync(userId);
            _dbContext.UserGames.Add(new UserGame { GameId = game.Id, UserId = userId, CollectionId = collection.Id });
            await _dbContext.SaveChangesAsync();

            var result = await _gameService.AddToUserCollectionAsync(game.Id, collection.Id, userId);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Game.AlreadyInCollection"));
        }

        [Test]
        public async Task AddToUserCollectionAsync_ValidRequest_AddsGameToCollection()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync();
            var collection = await SeedCollectionAsync(userId);

            var result = await _gameService.AddToUserCollectionAsync(game.Id, collection.Id, userId);

            Assert.That(result.IsSuccess, Is.True);
            var userGame = await _dbContext.UserGames.FirstOrDefaultAsync(ug => ug.GameId == game.Id && ug.UserId == userId);
            Assert.That(userGame, Is.Not.Null);
            Assert.That(userGame!.CollectionId, Is.EqualTo(collection.Id));
        }

        #endregion

        #region MoveGameAsync

        [Test]
        public async Task MoveGameAsync_GameRecordNotFound_ReturnsFailure()
        {
            var request = new MoveGameRequest { GameId = 1, CurrentCollectionId = 1, TargetCollectionId = 2 };

            var result = await _gameService.MoveGameAsync(request, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("GameRecord.NotFound"));
        }

        [Test]
        public async Task MoveGameAsync_TargetCollectionNotFound_ReturnsFailure()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync();
            var sourceCollection = await SeedCollectionAsync(userId, "Source");
            _dbContext.UserGames.Add(new UserGame { GameId = game.Id, UserId = userId, CollectionId = sourceCollection.Id });
            await _dbContext.SaveChangesAsync();
            var request = new MoveGameRequest { GameId = game.Id, CurrentCollectionId = sourceCollection.Id, TargetCollectionId = 9999 };

            var result = await _gameService.MoveGameAsync(request, userId);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("TargetCollection.NotFound"));
        }

        [Test]
        public async Task MoveGameAsync_ValidRequest_MovesGameToTargetCollection()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync();
            var sourceCollection = await SeedCollectionAsync(userId, "Source");
            var targetCollection = await SeedCollectionAsync(userId, "Target");
            _dbContext.UserGames.Add(new UserGame { GameId = game.Id, UserId = userId, CollectionId = sourceCollection.Id });
            await _dbContext.SaveChangesAsync();
            var request = new MoveGameRequest { GameId = game.Id, CurrentCollectionId = sourceCollection.Id, TargetCollectionId = targetCollection.Id };

            var result = await _gameService.MoveGameAsync(request, userId);

            Assert.That(result.IsSuccess, Is.True);
            var userGame = await _dbContext.UserGames.FirstAsync(ug => ug.GameId == game.Id && ug.UserId == userId);
            Assert.That(userGame.CollectionId, Is.EqualTo(targetCollection.Id));
        }

        #endregion

        #region RemoveFromCollectionAsync

        [Test]
        public async Task RemoveFromCollectionAsync_GameNotInCollection_ReturnsFailure()
        {
            var result = await _gameService.RemoveFromCollectionAsync(9999, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("UserGame.NotFound"));
        }

        [Test]
        public async Task RemoveFromCollectionAsync_GameInCollection_RemovesEntry()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync();
            var collection = await SeedCollectionAsync(userId);
            _dbContext.UserGames.Add(new UserGame { GameId = game.Id, UserId = userId, CollectionId = collection.Id });
            await _dbContext.SaveChangesAsync();

            var result = await _gameService.RemoveFromCollectionAsync(game.Id, userId);

            Assert.That(result.IsSuccess, Is.True);
            var entry = await _dbContext.UserGames.FirstOrDefaultAsync(ug => ug.GameId == game.Id && ug.UserId == userId);
            Assert.That(entry, Is.Null);
        }

        #endregion

        #region RateGameAsync

        [Test]
        public async Task RateGameAsync_RatingBelowMin_ReturnsFailure()
        {
            var result = await _gameService.RateGameAsync(1, 0, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Rating.Invalid"));
        }

        [Test]
        public async Task RateGameAsync_RatingAboveMax_ReturnsFailure()
        {
            var result = await _gameService.RateGameAsync(1, 11, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Rating.Invalid"));
        }

        [Test]
        public async Task RateGameAsync_GameNotFound_ReturnsFailure()
        {
            var result = await _gameService.RateGameAsync(9999, 8, "user-1");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Gra.NotFound"));
        }

        [Test]
        public async Task RateGameAsync_NewRating_CreatesEntry()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync(status: GameStatus.Accepted);

            var result = await _gameService.RateGameAsync(game.Id, 7, userId);

            Assert.That(result.IsSuccess, Is.True);
            var rating = await _dbContext.UserRating.FirstOrDefaultAsync(r => r.GameId == game.Id && r.UserId == userId);
            Assert.That(rating, Is.Not.Null);
            Assert.That(rating!.Value, Is.EqualTo(7));
        }

        [Test]
        public async Task RateGameAsync_ExistingRating_UpdatesValue()
        {
            const string userId = "user-1";
            var game = await SeedGameAsync(status: GameStatus.Accepted);
            _dbContext.UserRating.Add(new UserRating { GameId = game.Id, UserId = userId, Value = 5 });
            await _dbContext.SaveChangesAsync();

            var result = await _gameService.RateGameAsync(game.Id, 9, userId);

            Assert.That(result.IsSuccess, Is.True);
            var rating = await _dbContext.UserRating.FirstAsync(r => r.GameId == game.Id && r.UserId == userId);
            Assert.That(rating.Value, Is.EqualTo(9));
        }

        #endregion

        #region GetAllGenresAsync & GetAllPlatformsAsync

        [Test]
        public async Task GetAllGenresAsync_ReturnsAllGenresOrderedByName()
        {
            _dbContext.Genres.AddRange(new Genre { Name = "Shooter" }, new Genre { Name = "Adventure" }, new Genre { Name = "RPG" });
            await _dbContext.SaveChangesAsync();

            var result = await _gameService.GetAllGenresAsync();

            Assert.That(result.IsSuccess, Is.True);
            var genres = result.Data.ToList();
            Assert.That(genres.Count, Is.EqualTo(3));
            Assert.That(genres[0].Name, Is.EqualTo("Adventure"));
            Assert.That(genres[1].Name, Is.EqualTo("RPG"));
            Assert.That(genres[2].Name, Is.EqualTo("Shooter"));
        }

        [Test]
        public async Task GetAllPlatformsAsync_ReturnsAllPlatformsOrderedByName()
        {
            _dbContext.Platforms.AddRange(new Platform { Name = "Xbox" }, new Platform { Name = "PC" }, new Platform { Name = "PlayStation" });
            await _dbContext.SaveChangesAsync();

            var result = await _gameService.GetAllPlatformsAsync();

            Assert.That(result.IsSuccess, Is.True);
            var platforms = result.Data.ToList();
            Assert.That(platforms.Count, Is.EqualTo(3));
            Assert.That(platforms[0].Name, Is.EqualTo("PC"));
            Assert.That(platforms[1].Name, Is.EqualTo("PlayStation"));
            Assert.That(platforms[2].Name, Is.EqualTo("Xbox"));
        }

        #endregion

        #region GetPendingAsync

        [Test]
        public async Task GetPendingAsync_ReturnsOnlyPendingGames()
        {
            var genre = await SeedGenreAsync();
            var platform = await SeedPlatformAsync();
            await SeedGameAsync("Accepted", GameStatus.Accepted, genre.Id, platform.Id);
            await SeedGameAsync("Pending1", GameStatus.Pending, genre.Id, platform.Id);
            await SeedGameAsync("Pending2", GameStatus.Pending, genre.Id, platform.Id);
            await SeedGameAsync("Rejected", GameStatus.Rejected, genre.Id, platform.Id);

            var result = await _gameService.GetPendingAsync();

            Assert.That(result.IsSuccess, Is.True);
            var pending = result.Data.ToList();
            Assert.That(pending.Count, Is.EqualTo(2));
            Assert.That(pending.All(g => g.Status == GameStatus.Pending), Is.True);
        }

        #endregion
    }
}
