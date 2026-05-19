using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<SignInManager<ApplicationUser>> _signInManagerMock = null!;
        private Mock<ICollectionService> _collectionServiceMock = null!;
        private Mock<ILogger<AuthService>> _loggerMock = null!;
        private Mock<IConfiguration> _configurationMock = null!;
        private Mock<IWebHostEnvironment> _hostingEnvironmentMock = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
        private Mock<IFileService> _fileServiceMock = null!;
        private GameOrganizerDbContext _dbContext = null!;
        private AuthService _authService = null!;

        [SetUp]
        public void SetUp()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            var httpContextAccessorForSignIn = new Mock<IHttpContextAccessor>();
            var userClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                httpContextAccessorForSignIn.Object,
                userClaimsPrincipalFactory.Object,
                null, null, null, null);

            _collectionServiceMock = new Mock<ICollectionService>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _configurationMock = new Mock<IConfiguration>();
            _hostingEnvironmentMock = new Mock<IWebHostEnvironment>();
            _emailSenderMock = new Mock<IEmailSender>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _fileServiceMock = new Mock<IFileService>();

            var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new GameOrganizerDbContext(options);

            _authService = new AuthService(
                _userManagerMock.Object,
                _collectionServiceMock.Object,
                _loggerMock.Object,
                _configurationMock.Object,
                _signInManagerMock.Object,
                _hostingEnvironmentMock.Object,
                _emailSenderMock.Object,
                _httpContextAccessorMock.Object,
                _fileServiceMock.Object,
                _dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        #region RegisterAsync

        [Test]
        public async Task RegisterAsync_EmailIsEmpty_ReturnsFailure()
        {
            var dto = new RegisterDto { Email = "", Username = "user1", Password = "Password1!" };

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.EmailRequired"));
        }

        [Test]
        public async Task RegisterAsync_EmailIsWhitespace_ReturnsFailure()
        {
            var dto = new RegisterDto { Email = "   ", Username = "user1", Password = "Password1!" };

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.EmailRequired"));
        }

        [Test]
        public async Task RegisterAsync_EmailAlreadyInUse_ReturnsFailure()
        {
            var existingUser = new ApplicationUser { Email = "existing@test.com", UserName = "existing" };
            var dto = new RegisterDto { Email = "existing@test.com", Username = "newuser", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(existingUser);

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.UserExists"));
        }

        [Test]
        public async Task RegisterAsync_UsernameTaken_ReturnsFailure()
        {
            var existingUser = new ApplicationUser { Email = "other@test.com", UserName = "takenuser" };
            var dto = new RegisterDto { Email = "new@test.com", Username = "takenuser", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync(existingUser);

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.UsernameTaken"));
        }

        [Test]
        public async Task RegisterAsync_UserCreationFails_ReturnsFailure()
        {
            var dto = new RegisterDto { Email = "new@test.com", Username = "newuser", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.CreationFailed"));
        }

        [Test]
        public async Task RegisterAsync_FirstUserGetsAdminRole_ReturnsSuccess()
        {
            var dto = new RegisterDto { Email = "admin@test.com", Username = "admin", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Administrator"))
                .ReturnsAsync(new List<ApplicationUser>());
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Administrator"))
                .ReturnsAsync(IdentityResult.Success);
            _collectionServiceMock.Setup(x => x.InitDefaultCollectionsAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult.Success());

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsSuccess, Is.True);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Administrator"), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_SubsequentUserGetsUserRole_ReturnsSuccess()
        {
            var dto = new RegisterDto { Email = "user@test.com", Username = "regularuser", Password = "Password1!" };
            var admins = new List<ApplicationUser> { new ApplicationUser { UserName = "existingAdmin" } };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetUsersInRoleAsync("Administrator"))
                .ReturnsAsync(admins);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);
            _collectionServiceMock.Setup(x => x.InitDefaultCollectionsAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult.Success());

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result.IsSuccess, Is.True);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
        }

        #endregion

        #region Login

        [Test]
        public async Task Login_UserNotFound_ReturnsFailure()
        {
            var dto = new LoginDto { Email = "notfound@test.com", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            var result = await _authService.Login(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.InvalidCredentials"));
        }

        [Test]
        public async Task Login_AccountLockedOut_ReturnsFailure()
        {
            var user = new ApplicationUser { Email = "user@test.com", UserName = "user" };
            var dto = new LoginDto { Email = "user@test.com", Password = "WrongPassword" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var result = await _authService.Login(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Description, Does.Contain("zablokowane"));
        }

        [Test]
        public async Task Login_InvalidPassword_ReturnsFailure()
        {
            var user = new ApplicationUser { Email = "user@test.com", UserName = "user" };
            var dto = new LoginDto { Email = "user@test.com", Password = "WrongPassword" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var result = await _authService.Login(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.InvalidCredentials"));
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsSuccessWithToken()
        {
            var user = new ApplicationUser { Id = "user-id", Email = "user@test.com", UserName = "user" };
            var dto = new LoginDto { Email = "user@test.com", Password = "CorrectPassword!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            _configurationMock.Setup(x => x["JWT:Secret"])
                .Returns("super-secret-key-that-is-long-enough-for-hmac");
            _configurationMock.Setup(x => x["JWT:Issuer"])
                .Returns("TestIssuer");
            _configurationMock.Setup(x => x["JWT:Audience"])
                .Returns("TestAudience");

            var result = await _authService.Login(dto);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Token, Is.Not.Empty);
        }

        #endregion

        #region ResetPasswordAsync

        [Test]
        public async Task ResetPasswordAsync_UserNotFound_ReturnsFailure()
        {
            var dto = new ResetPasswordDto { Email = "notfound@test.com", Token = "token", NewPassword = "NewPass1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            var result = await _authService.ResetPasswordAsync(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Request.BadRequest"));
        }

        [Test]
        public async Task ResetPasswordAsync_InvalidToken_ReturnsFailure()
        {
            var user = new ApplicationUser { Email = "user@test.com", UserName = "user" };
            var dto = new ResetPasswordDto { Email = "user@test.com", Token = "invalid-token", NewPassword = "NewPass1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

            var result = await _authService.ResetPasswordAsync(dto);

            Assert.That(result.IsFailure, Is.True);
        }

        [Test]
        public async Task ResetPasswordAsync_ValidRequest_ReturnsSuccess()
        {
            var user = new ApplicationUser { Email = "user@test.com", UserName = "user" };
            var dto = new ResetPasswordDto { Email = "user@test.com", Token = "valid-token", NewPassword = "NewPass1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.ResetPasswordAsync(dto);

            Assert.That(result.IsSuccess, Is.True);
        }

        #endregion

        #region CreateAdminUser

        [Test]
        public async Task CreateAdminUser_EmailAlreadyInUse_ReturnsFailure()
        {
            var existing = new ApplicationUser { Email = "admin@test.com", UserName = "admin" };
            var dto = new RegisterDto { Email = "admin@test.com", Username = "newadmin", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(existing);

            var result = await _authService.CreateAdminUser(dto);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Auth.UserExists"));
        }

        [Test]
        public async Task CreateAdminUser_Success_AssignsAdminRole()
        {
            var dto = new RegisterDto { Email = "admin@test.com", Username = "admin", Password = "Password1!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Administrator"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.CreateAdminUser(dto);

            Assert.That(result.IsSuccess, Is.True);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Administrator"), Times.Once);
        }

        #endregion
    }
}
