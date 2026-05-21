using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Tests;

[TestFixture]
public class UserManagementServiceTests
{
    private SqliteConnection _connection = null!;
    private GameOrganizerDbContext _dbContext = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private RoleManager<IdentityRole> _roleManager = null!;
    private UserManagementService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new GameOrganizerDbContext(options);
        _dbContext.Database.EnsureCreated();

        _userManager = BuildUserManager(_dbContext);
        _roleManager = BuildRoleManager(_dbContext);

        _service = new UserManagementService(
            _dbContext,
            new Mock<ILogger<UserManagementService>>().Object,
            _userManager,
            _roleManager);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
        _roleManager.Dispose();
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task GetAllRolesAsync_WhenRolesExist_ReturnsSuccess()
    {
        _dbContext.Roles.AddRange(
            new IdentityRole { Id = "r1", Name = "User", NormalizedName = "USER" },
            new IdentityRole { Id = "r2", Name = "Administrator", NormalizedName = "ADMINISTRATOR" });
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllRolesAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserById_WhenUserMissing_ReturnsFailure()
    {
        var result = await _service.GetUserById("missing-user");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("User.NotFound"));
    }

    [Test]
    public async Task CreateUser_WhenEmailMissing_ReturnsFailure()
    {
        var dto = new UserDto
        {
            UserName = "new-user",
            Email = "",
            RoleId = "r-user"
        };

        var result = await _service.CreateUser(dto, CreatePrincipal("Administrator"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.EmailRequired"));
    }

    [Test]
    public async Task CreateUser_WhenRoleMissing_ReturnsFailure()
    {
        var dto = new UserDto
        {
            UserName = "new-user",
            Email = "new@site.com",
            RoleId = "missing-role"
        };

        var result = await _service.CreateUser(dto, CreatePrincipal("Administrator"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("UserMgmt.RoleNotFound"));
    }

    [Test]
    public async Task CreateUser_WhenAdminRoleAndCurrentUserNotAdmin_ReturnsFailure()
    {
        _dbContext.Roles.Add(new IdentityRole
        {
            Id = "r-admin",
            Name = "Administrator",
            NormalizedName = "ADMINISTRATOR"
        });
        await _dbContext.SaveChangesAsync();

        var dto = new UserDto
        {
            UserName = "new-admin",
            Email = "admin@site.com",
            RoleId = "r-admin"
        };

        var result = await _service.CreateUser(dto, CreatePrincipal("User"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.PrivilegesRequired"));
    }

    [Test]
    public async Task CreateUser_WhenValid_CreatesAndAssignsRole()
    {
        _dbContext.Roles.Add(new IdentityRole
        {
            Id = "r-user",
            Name = "User",
            NormalizedName = "USER"
        });
        await _dbContext.SaveChangesAsync();

        var dto = new UserDto
        {
            UserName = "new-user",
            Email = "new@site.com",
            RoleId = "r-user"
        };

        var result = await _service.CreateUser(dto, CreatePrincipal("Administrator"));

        Assert.That(result.IsSuccess, Is.True);
        var createdUser = await _userManager.FindByEmailAsync("new@site.com");
        Assert.That(createdUser, Is.Not.Null);
        Assert.That(await _userManager.IsInRoleAsync(createdUser!, "User"), Is.True);
    }

    [Test]
    public async Task LockUser_WhenMissing_ReturnsFailure()
    {
        var result = await _service.LockUser("missing-id");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("User.NotFound"));
    }

    [Test]
    public async Task LockUser_WhenExists_SetsPermanentLockout()
    {
        var user = await CreateIdentityUser("lock@site.com", "lock-user");

        var result = await _service.LockUser(user.Id);

        Assert.That(result.IsSuccess, Is.True);
        var updated = await _userManager.FindByIdAsync(user.Id);
        Assert.That(updated!.LockoutEnd.HasValue, Is.True);
        Assert.That(updated.LockoutEnd!.Value, Is.GreaterThan(DateTimeOffset.UtcNow.AddYears(10)));
    }

    [Test]
    public async Task UnlockUser_WhenExists_ClearsLockout()
    {
        var user = await CreateIdentityUser("unlock@site.com", "unlock-user");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(7));

        var result = await _service.UnlockUser(user.Id);

        Assert.That(result.IsSuccess, Is.True);
        var updated = await _userManager.FindByIdAsync(user.Id);
        Assert.That(updated!.LockoutEnd.HasValue, Is.False);
    }

    [Test]
    public async Task UpdateUser_WhenEmailUsedByAnotherUser_ReturnsFailure()
    {
        var role = new IdentityRole { Id = "r-user", Name = "User", NormalizedName = "USER" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        var userA = await CreateIdentityUser("a@site.com", "user-a");
        var userB = await CreateIdentityUser("b@site.com", "user-b");
        await _userManager.AddToRoleAsync(userA, "User");

        var dto = new UserDto
        {
            UserId = userA.Id,
            UserName = "user-a-updated",
            Email = userB.Email!,
            RoleId = role.Id
        };

        var result = await _service.UpdateUser(dto);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.UserExists"));
    }

    [Test]
    public async Task UpdateUser_WhenValid_UpdatesFieldsAndRole()
    {
        var userRole = new IdentityRole { Id = "r-user", Name = "User", NormalizedName = "USER" };
        var adminRole = new IdentityRole { Id = "r-admin", Name = "Administrator", NormalizedName = "ADMINISTRATOR" };
        _dbContext.Roles.AddRange(userRole, adminRole);
        await _dbContext.SaveChangesAsync();

        var user = await CreateIdentityUser("old@site.com", "old-name");
        await _userManager.AddToRoleAsync(user, "User");

        var dto = new UserDto
        {
            UserId = user.Id,
            UserName = "new-name",
            Email = "new@site.com",
            RoleId = adminRole.Id
        };

        var result = await _service.UpdateUser(dto);

        Assert.That(result.IsSuccess, Is.True);

        var updated = await _userManager.FindByIdAsync(user.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.UserName, Is.EqualTo("new-name"));
        Assert.That(updated.Email, Is.EqualTo("new@site.com"));
        Assert.That(await _userManager.IsInRoleAsync(updated, "Administrator"), Is.True);
        Assert.That(await _userManager.IsInRoleAsync(updated, "User"), Is.False);
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "current-user"),
            new Claim(ClaimTypes.Role, role)
        }, "Test");

        return new ClaimsPrincipal(identity);
    }

    private async Task<ApplicationUser> CreateIdentityUser(string email, string userName)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            EmailConfirmed = true
        };

        var create = await _userManager.CreateAsync(user, "StrongPass123!");
        Assert.That(create.Succeeded, Is.True, string.Join(", ", create.Errors.Select(e => e.Description)));

        return user;
    }

    private static UserManager<ApplicationUser> BuildUserManager(GameOrganizerDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole, GameOrganizerDbContext, string>(context);

        return new UserManager<ApplicationUser>(
            store,
            null,
            new PasswordHasher<ApplicationUser>(),
            new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() },
            new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            new Logger<UserManager<ApplicationUser>>(new LoggerFactory()));
    }

    private static RoleManager<IdentityRole> BuildRoleManager(GameOrganizerDbContext context)
    {
        var roleStore = new RoleStore<IdentityRole, GameOrganizerDbContext, string>(context);

        return new RoleManager<IdentityRole>(
            roleStore,
            new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Logger<RoleManager<IdentityRole>>(new LoggerFactory()));
    }
}
