using GameOrganizer.Api.Hubs;
using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Models.Dto.Collections;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using GameOrganizer.Api.Models.Dto.Users;


namespace GameOrganizer.Api.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly GameOrganizerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<FriendService> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private const string ObjectName = "Collection";

        public CollectionService(GameOrganizerDbContext context, UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContex,
            IConfiguration configuration,
            IEmailSender emailSender,
            ILogger<FriendService> logger,
            IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContex;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }
        public async Task<ServiceResult> InitDefaultCollectionsAsync(string userId)
        {
            if (await _context.Collections.AnyAsync(c => c.UserId == userId))
                return ServiceResult.Success();

            var names = new[] { "Ulubione", "Planowane", "Lista życzeń", "W trakcie", "Ukończone", "Porzucone" };

            var collections = names.Select(name => new Collection
            {
                Name = name,
                UserId = userId,
                IsPublic = true,
                ShareCode = Guid.NewGuid()
            }).ToList();

            _context.Collections.AddRange(collections);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<DataTableResponse<CollectionWithGamesDto>>> GetMyGamesAsync(string userId, DataTableRequest request)
        {
            try
            {
                var query = _context.UserGamesView
                    .Where(v => v.UserId == userId)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchValue))
                {
                    string sv = request.SearchValue.ToLower();
                    query = query.Where(v =>
                        v.Title.ToLower().Contains(sv) ||
                        v.GenreName.ToLower().Contains(sv) ||
                        v.PlatformName.ToLower().Contains(sv) ||
                        v.CollectionName.ToLower().Contains(sv));
                }

                var flatResults = await query.ToListAsync();

                var gameIds = flatResults.Select(f => f.GameId).Distinct().ToList();

                var averages = await _context.UserRating
                    .Where(r => gameIds.Contains(r.GameId))
                    .GroupBy(r => r.GameId)
                    .Select(g => new { GameId = g.Key, Avg = g.Average(r => (double)r.Value) })
                    .ToDictionaryAsync(x => x.GameId, x => Math.Round(x.Avg, 1));

                var myRatings = await _context.UserRating
                    .Where(r => gameIds.Contains(r.GameId) && r.UserId == userId)
                    .ToDictionaryAsync(x => x.GameId, x => x.Value);

                var groupedData = flatResults
                    .GroupBy(v => new { v.CollectionId, v.CollectionName, v.IsPublic })
                    .Select(g => new CollectionWithGamesDto
                    {
                        CollectionId = g.Key.CollectionId,
                        CollectionName = g.Key.CollectionName,
                        IsPublic = g.Key.IsPublic,
                        Games = g.Select(v => new UserGameDto
                        {
                            GameId = v.GameId,
                            Title = v.Title,
                            GenreName = v.GenreName,
                            PlatformName = v.PlatformName,
                            AddedAt = v.AddedAt,
                            AverageRating = averages.ContainsKey(v.GameId) ? averages[v.GameId] : 0,
                            MyRating = myRatings.ContainsKey(v.GameId) ? myRatings[v.GameId] : (int?)null
                        }).OrderBy(x => x.Title).ToList()
                    })
                    .AsQueryable();

                var totalRecords = await _context.Collections.CountAsync(c => c.UserId == userId);
                var recordsFiltered = groupedData.Count();

                var pagedData = groupedData
                    .OrderBy(x => x.CollectionName) 
                    .Skip(request.Start)
                    .Take(request.Length)
                    .ToList();

                return ServiceResult<DataTableResponse<CollectionWithGamesDto>>.Success(new DataTableResponse<CollectionWithGamesDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = recordsFiltered,
                    Data = pagedData
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<DataTableResponse<CollectionWithGamesDto>>.Failure(CommonErrors.DataProcessingError());
            }
        }

        public async Task<ServiceResult<Collection>> CreateCollectionAsync(CollectionDto dto, string userId)
        {
            var collection = new Collection
            {
                Name = dto.Name,
                IsPublic = dto.IsPublic,
                UserId = userId,
                ShareCode = Guid.NewGuid()
            };

            _context.Collections.Add(collection);
            await _context.SaveChangesAsync();
            return ServiceResult<Collection>.Success(collection);
        }

        public async Task<ServiceResult> UpdateCollectionAsync(CollectionDto dto, string userId)
        {
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.UserId == userId);

            if (collection == null)
                return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, dto.Id ?? 0));

            collection.Name = dto.Name;
            collection.IsPublic = dto.IsPublic;

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteCollectionAsync(int id, string userId)
        {
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (collection == null)
                return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, id));

            _context.Collections.Remove(collection);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<List<CollectionDto>>> GetUserCollectionsLookupAsync(string userId)
        {
            var collections = await _context.Collections
                .Where(c => c.UserId == userId)
                .Select(c => new CollectionDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsPublic = c.IsPublic
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return ServiceResult<List<CollectionDto>>.Success(collections);
        }

        public async Task<ServiceResult<SharedCollectionDto>> GetSharedCollectionAsync(Guid shareCode)
        {
            var collection = await _context.Collections
                .Include(c => c.User)
                .Include(c => c.UserGames)
                    .ThenInclude(ug => ug.Game)
                        .ThenInclude(g => g.Genre)
                .Include(c => c.UserGames)
                    .ThenInclude(ug => ug.Game)
                        .ThenInclude(g => g.Platform)
                .FirstOrDefaultAsync(c => c.ShareCode == shareCode && c.IsPublic); 

            if (collection == null)
            {
                return ServiceResult<SharedCollectionDto>.Failure(
                    new ServiceError("Collection.NotShared", "Ta kolekcja nie została udostępniona lub jest prywatna.")
                );
            }

            var dto = new SharedCollectionDto
            {
                CollectionName = collection.Name,
                OwnerName = collection.User.UserName ?? "Anonimowy Użytkownik",
                Games = collection.UserGames.Select(ug => new UserGameDto
                {
                    GameId = ug.GameId,
                    Title = ug.Game.Title,
                    GenreName = ug.Game.Genre.Name,
                    PlatformName = ug.Game.Platform.Name,
                    AddedAt = ug.AddedAt
                })
                .OrderBy(g => g.Title)
                .ToList()
            };

            return ServiceResult<SharedCollectionDto>.Success(dto);
        }

        public async Task<ServiceResult> ShareCollectionByEmailAsync(int collectionId, string ownerId, string recipientEmail)
        {
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == ownerId);

            if (collection == null) return ServiceResult.Failure(CommonErrors.NotFound("Kolekcja", collectionId));

            if (!collection.IsPublic)
            {
                return ServiceResult.Failure(new ServiceError(
                    "Collection.Private",
                    "Nie można udostępnić kolekcji prywatnej. Najpierw zmień status kolekcji na publiczny."
                ));
            }

            var owner = await _userManager.FindByIdAsync(ownerId);
            var baseUrl = _configuration["FRONTEND_BASE_URL"] ?? "http://localhost:5173";

            var shareUrl = $"{baseUrl}/shared-collection/{collection.ShareCode}";

            try
            {
                var templatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Templates", "ShareCollectionEmail.html");
                var emailBody = await File.ReadAllTextAsync(templatePath);

                emailBody = emailBody.Replace("{SenderName}", owner?.UserName ?? "Znajomy")
                                     .Replace("{ShareUrl}",  shareUrl);

                var subject = $"{owner?.UserName} udostępnił Ci swoją kolekcję gier!";

                await _emailSender.SendEmailAsync(recipientEmail, subject, emailBody);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania linku do kolekcji");
                return ServiceResult.Failure(new ServiceError("Email.Failed", "Nie udało się wysłać maila z kolekcją."));
            }
        }

    }
}

