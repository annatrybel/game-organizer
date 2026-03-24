using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Claims;
using System.Text.Json;
using GameOrganizer.Api.Models.View;

namespace GameOrganizer.Api.Models
{
    public class GameOrganizerDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GameOrganizerDbContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public GameOrganizerDbContext() { }
        public GameOrganizerDbContext(DbContextOptions<GameOrganizerDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public GameOrganizerDbContext(DbContextOptions<GameOrganizerDbContext> options) : base(options)
        {
        }

       
        public DbSet<HistoryLog> HistoryLogs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<UserGame> UserGames { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<UserGamesView> UserGamesView { get; set; }
        public DbSet<Friendship> Friendship { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDto>().HasNoKey().ToView("UserWithRoles");

            modelBuilder.Entity<HistoryLog>()
                .Property(h => h.CreationDate)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<ChatMessage>()
               .Property(h => h.Timestamp)
               .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<UserGame>()  //User usuwa folder -> znikają wpisy gier w tym folderze
                .HasOne(ug => ug.Collection)
                .WithMany(c => c.UserGames)
                .HasForeignKey(ug => ug.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserGame>()  //Admin usuwa grę globalnie -> znika z kolekcji wszystkich userów
                .HasOne(ug => ug.Game)
                .WithMany() 
                .HasForeignKey(ug => ug.GameId)
                .OnDelete(DeleteBehavior.Cascade);
                        
            modelBuilder.Entity<Collection>()  //User usuwa konto->usuwają się jego kolekcje
                .HasOne(c => c.User)
                .WithMany() 
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserGamesView>()
               .ToView("UserGamesView")
               .HasNoKey();

            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasOne(f => f.Requester)
                    .WithMany()
                    .HasForeignKey(f => f.RequesterId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Receiver)
                    .WithMany()
                    .HasForeignKey(f => f.ReceiverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(f => new { f.RequesterId, f.ReceiverId }).IsUnique();
            });
        }

        private async Task<ApplicationUser?> GetIdentityUser()
        {
            ApplicationUser? identityUser = null;
            var userIdentity = _httpContextAccessor?.HttpContext?.User?.Identity;
            if (userIdentity != null)
            {
                var claimsUserIdentity = (ClaimsIdentity)userIdentity;
                var userNameIdentifier = claimsUserIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (userNameIdentifier != null)
                {
                    identityUser = await Users.FirstAsync(u => u.Id == userNameIdentifier.Value);
                }
            }

            return identityUser;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.DetectChanges();

            var entries = ChangeTracker
                .Entries()
                .Where(e =>
                        (e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted) &&
                        e.Entity.GetType() != typeof(HistoryLog)
                        );

            ApplicationUser? identityUser = null;
            bool isAnyEntityChanged = entries.Any();
            if (isAnyEntityChanged)
                identityUser = await GetIdentityUser();

            var historyLogs = new List<HistoryLog>();
            var addedEntities = new Dictionary<EntityEntry, HistoryLog>();

            foreach (var entry in entries)
            {
                if (entry.Metadata.FindPrimaryKey().Properties.Count > 1) //złożony klucz
                {
                    continue;
                }


                string typeName = entry.Entity.GetType() == typeof(ApplicationUser)
                    ? "User"
                    : entry.Entity.GetType().Name;

                bool skipHistoryLog = false;
                string before = string.Empty;
                string after = string.Empty;

                switch (entry.State)
                {
                    case EntityState.Added:
                        var (afterJson, isAfterEmpty) = SerializeEntityChanges(entry, EntityState.Added);
                        if (isAfterEmpty)
                        {
                            skipHistoryLog = true;
                            break;
                        }
                        after = afterJson;
                        break;

                    case EntityState.Modified:
                        var (beforeModifiedJson, isBeforeModifiedEmpty) = SerializeEntityChanges(entry, EntityState.Modified, "Before");
                        var (afterModifiedJson, isAfterModifiedEmpty) = SerializeEntityChanges(entry, EntityState.Modified, "After");

                        if (isBeforeModifiedEmpty && isAfterModifiedEmpty)
                        {
                            skipHistoryLog = true;
                            break;
                        }
                        before = beforeModifiedJson;
                        after = afterModifiedJson;
                        break;

                    case EntityState.Deleted:
                        var (beforeJson, isBeforeEmpty) = SerializeEntityChanges(entry, EntityState.Added);
                        if (isBeforeEmpty)
                        {
                            skipHistoryLog = true;
                            break;
                        }
                        before = beforeJson;
                        break;
                }

                if (skipHistoryLog)
                    continue;

                var historyLog = new HistoryLog
                {
                    CreationDate = DateTime.UtcNow,
                    ObjectId = GetPrimaryKeyValue(entry).ToString() ?? "UnknownId",
                    ObjectType = typeName,
                    UserEmail = identityUser?.Email,
                    UserId = identityUser?.Id,
                    EventType = $"{entry.State} {typeName}",
                    Before = before,
                    After = after,
                };

                if (entry.State == EntityState.Added)
                {
                    addedEntities.Add(entry, historyLog);
                }

                historyLogs.Add(historyLog);
            }

            if (historyLogs.Any())
            {
                await HistoryLogs.AddRangeAsync(historyLogs, cancellationToken);
            }
            var result = await base.SaveChangesAsync(cancellationToken);

            if (addedEntities.Any())
            {
                foreach (var addedEntity in addedEntities)
                {
                    var entry = addedEntity.Key;
                    var historyLog = addedEntity.Value;
                    var realId = GetPrimaryKeyValue(entry)?.ToString() ?? "UnknownId";

                    if (historyLog.ObjectId != realId)
                    {
                        historyLog.ObjectId = realId;
                    }
                }
                if (historyLogs.Any())
                {
                    await base.SaveChangesAsync(cancellationToken);
                }
            }
            return result;
        }

        private (string json, bool isEmpty) SerializeEntityChanges(EntityEntry entry, EntityState state, string version = "")
        {
            try
            {
                var changes = new Dictionary<string, object>();

                string[] userFieldsToExclude = { "ConcurrencyStamp", "AccessFailedCount", "PasswordHash", "SecurityStamp" };

                bool isIdentityUser = entry.Entity.GetType().Equals(typeof(ApplicationUser));

                if (state == EntityState.Added)
                {
                    foreach (var property in entry.CurrentValues.Properties)
                    {
                        if (property.Name != "Id" && property.Name != "CreatedAt" &&
                            !(isIdentityUser && userFieldsToExclude.Contains(property.Name)))
                        {
                            changes.Add(property.Name, entry.CurrentValues[property] ?? DBNull.Value);
                        }
                    }
                }
                else if (state == EntityState.Deleted)
                {
                    foreach (var property in entry.OriginalValues.Properties)
                    {
                        if (property.Name != "Id" && property.Name != "CreatedAt" &&
                            !(isIdentityUser && userFieldsToExclude.Contains(property.Name)))
                        {
                            changes.Add(property.Name, entry.OriginalValues[property] ?? DBNull.Value);
                        }
                    }
                }
                else if (state == EntityState.Modified)
                {
                    foreach (var property in entry.OriginalValues.Properties)
                    {
                        var originalValue = entry.OriginalValues[property];
                        var currentValue = entry.CurrentValues[property];

                        if (!object.Equals(originalValue, currentValue) &&
                            !(isIdentityUser && userFieldsToExclude.Contains(property.Name)))
                        {
                            if (version == "Before")
                            {
                                changes.Add(property.Name, originalValue ?? DBNull.Value);
                            }
                            else
                            {
                                changes.Add(property.Name, currentValue ?? DBNull.Value);
                            }
                        }
                    }
                }
                return (JsonSerializer.Serialize(changes), changes.Count == 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing changes: {ex}");

                return (string.Empty, true);
            }
        }


        private object GetPrimaryKeyValue(EntityEntry entry)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            var keyName = primaryKey.Properties.Single().Name;
            return entry.Property(keyName).CurrentValue;
        }
    }
}
