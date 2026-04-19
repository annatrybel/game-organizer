using GameOrganizer.Api.Hubs;
using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace GameOrganizer.Api.Services
{
    public class FriendService : IFriendService
    {
        private readonly GameOrganizerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<FriendService> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public FriendService(GameOrganizerDbContext context, 
            UserManager<ApplicationUser> userManager,
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

        public async Task<ServiceResult> SendFriendRequestAsync(string requesterId, string targetUsername)
        {
            var targetUser = await _userManager.FindByNameAsync(targetUsername);
            if (targetUser == null) return ServiceResult.Failure(CommonErrors.NotFound("Użytkownik", targetUsername));
            if (targetUser.Id == requesterId) return ServiceResult.Failure(new ServiceError("Friends.Self", "Nie możesz dodać samego siebie do znajomych."));

            var requesterUser = await _userManager.FindByIdAsync(requesterId);
            if (requesterUser == null)
                return ServiceResult.Failure(CommonErrors.NotFound("Nadawca", requesterId));

            var existing = await _context.Set<Friendship>().FirstOrDefaultAsync(f =>
                (f.RequesterId == requesterId && f.ReceiverId == targetUser.Id) ||
                (f.RequesterId == targetUser.Id && f.ReceiverId == requesterId));

            if (existing != null)
                return ServiceResult.Failure(new ServiceError("Friends.AlreadyExists", "Zaproszenie już istnieje lub jesteście znajomymi."));

            var friendship = new Friendship {
                RequesterId = requesterId, 
                ReceiverId = targetUser.Id,
                Status = FriendshipStatus.Pending
            };

            _context.Friendship.Add(friendship);

            var notification = new Notification
            {
                UserId = targetUser.Id,
                Message = $"Użytkownik {requesterUser.UserName} wysłał Ci zaproszenie do znajomych.",
                Type = "FriendRequest",
                ExtraData = requesterId
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(targetUser.Id).SendAsync("ReceiveNotification", new
            {
                message = notification.Message,
                type = notification.Type,
                senderId = requesterId
            });

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IEnumerable<FriendDto>>> GetFriendsAsync(string userId)
        {
            var friends = await _context.Set<Friendship>()
                .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.Receiver : f.Requester)
                .Select(u => new FriendDto
                {
                    UserId = u.Id,
                    UserName = u.UserName!,
                    AvatarUrl = u.AvatarUrl,
                    Status = "Accepted"
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<FriendDto>>.Success(friends);
        }

        public async Task<ServiceResult> SendInviteEmailAsync(string userId, string recipientEmail)
        {
            var sender = await _userManager.FindByIdAsync(userId);
            if (sender == null) return ServiceResult.Failure(CommonErrors.NotFound("User", userId));

            var baseUrl = _configuration["FRONTEND_BASE_URL"] ?? "http://localhost:5173";

            var registrationUrl = $"{baseUrl}/register";

            try
            {
                var templatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Templates", "InvitationEmail.html");
                var emailBody = await File.ReadAllTextAsync(templatePath);

                emailBody = emailBody.Replace("{SenderName}", sender.UserName ?? "Znajomy")
                                     .Replace("{InviteUrl}", registrationUrl);

                var subject = $"{sender.UserName} zaprasza Cię do GameShelf";

                await _emailSender.SendEmailAsync(recipientEmail, subject, emailBody);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Błąd wysyłania zaproszenia");
                return ServiceResult.Failure(new ServiceError("Email.Failed", "Nie udało się wysłać zaproszenia."));
            }
        }

        public async Task<ServiceResult<DataTableResponse<UserSearchResultDto>>> SearchUsersAsync(string currentUserId, DataTableRequest request)
        {
            try
            {
                var baseQuery = _userManager.Users
                    .Where(u => u.Id != currentUserId); 

                if (!string.IsNullOrEmpty(request.SearchValue))
                {
                    var sv = request.SearchValue.ToLower();
                    baseQuery = baseQuery.Where(u => u.UserName!.ToLower().Contains(sv) || u.Email!.ToLower().Contains(sv));
                }

                var totalRecords = await baseQuery.CountAsync();

                var users = await baseQuery
                    .Skip(request.Start)
                    .Take(request.Length)
                    .ToListAsync();

                var myFriendships = await _context.Friendship
                    .Where(f => f.RequesterId == currentUserId || f.ReceiverId == currentUserId)
                    .ToListAsync();

                var resultData = users.Select(u => {
                    var rel = myFriendships.FirstOrDefault(f => f.RequesterId == u.Id || f.ReceiverId == u.Id);
                    string status = "None";

                    if (rel != null)
                    {
                        if (rel.Status == FriendshipStatus.Accepted) status = "Accepted";
                        else if (rel.Status == FriendshipStatus.Rejected) status = "Rejected";
                        else 
                        {
                            status = rel.RequesterId == currentUserId ? "PendingSent" : "PendingReceived";
                        }
                    }

                    return new UserSearchResultDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        AvatarUrl = u.AvatarUrl,
                        RelationStatus = status
                    };
                }).ToList();

                return ServiceResult<DataTableResponse<UserSearchResultDto>>.Success(new DataTableResponse<UserSearchResultDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = totalRecords,
                    Data = resultData
                });
            }
            catch (Exception) { return ServiceResult<DataTableResponse<UserSearchResultDto>>.Failure(CommonErrors.DataProcessingError()); }
        }

        public async Task<ServiceResult> AcceptFriendRequestAsync(string currentUserId, string requesterId)
        {
            var friendship = await _context.Friendship
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.ReceiverId == currentUserId && f.Status == FriendshipStatus.Pending);

            if (friendship == null) return ServiceResult.Failure(CommonErrors.NotFound("Zaproszenie", requesterId));

            friendship.Status = FriendshipStatus.Accepted;
            await _context.SaveChangesAsync();

            var me = await _userManager.FindByIdAsync(currentUserId);
            await _hubContext.Clients.User(requesterId).SendAsync("ReceiveNotification", new
            {
                message = $"Użytkownik {me?.UserName} zaakceptował Twoje zaproszenie!",
                type = "FriendAcceptance"
            });

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RejectFriendRequestAsync(string currentUserId, string requesterId)
        {
            var friendship = await _context.Friendship
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.ReceiverId == currentUserId && f.Status == FriendshipStatus.Pending);

            if (friendship == null) return ServiceResult.Failure(CommonErrors.NotFound("Zaproszenie", requesterId));

            friendship.Status = FriendshipStatus.Rejected; 
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }


        public async Task<ServiceResult<IEnumerable<FriendDto>>> GetIncomingRequestsAsync(string userId)
        {
            var requests = await _context.Friendship
                .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Requester)
                .Select(f => new FriendDto
                {
                    UserId = f.RequesterId,
                    UserName = f.Requester.UserName!,
                    AvatarUrl = f.Requester.AvatarUrl,
                    Status = "Pending"
                }).ToListAsync();

            return ServiceResult<IEnumerable<FriendDto>>.Success(requests);
        }

        public async Task<ServiceResult<IEnumerable<CollectionWithGamesDto>>> GetFriendCollectionsWithGamesAsync(string currentUserId, string friendId)
        {
            var areFriends = await _context.Friendship
                .AnyAsync(f => ((f.RequesterId == currentUserId && f.ReceiverId == friendId) ||
                                (f.RequesterId == friendId && f.ReceiverId == currentUserId))
                               && f.Status == FriendshipStatus.Accepted);

            if (!areFriends)
                return ServiceResult<IEnumerable<CollectionWithGamesDto>>.Failure(CommonErrors.Forbidden());

            var result = await _context.Collections
                .Where(c => c.UserId == friendId && c.IsPublic) 
                .OrderBy(c => c.Name)
                .Select(c => new CollectionWithGamesDto
                {
                    CollectionId = c.Id,
                    CollectionName = c.Name,
                    IsPublic = c.IsPublic,
                    Games = _context.UserGames
                        .Where(ug => ug.CollectionId == c.Id)
                        .Select(ug => new UserGameDto
                        {
                            GameId = ug.GameId,
                            Title = ug.Game.Title,
                            Description = ug.Game.Description,
                            GenreName = ug.Game.Genre.Name,
                            PlatformName = ug.Game.Platform.Name,
                            CollectionId = c.Id,
                            CollectionName = c.Name,
                            AddedAt = ug.AddedAt
                        })
                        .OrderBy(g => g.Title)
                        .ToList()
                        })
                .ToListAsync();

            return ServiceResult<IEnumerable<CollectionWithGamesDto>>.Success(result);
        }

        public async Task<ServiceResult<List<GameComparisonDto>>> CompareGamesWithFriendAsync(string currentUserId, string friendId)
        {
            var areFriends = await _context.Friendship
                .AnyAsync(f => ((f.RequesterId == currentUserId && f.ReceiverId == friendId) ||
                                (f.RequesterId == friendId && f.ReceiverId == currentUserId))
                               && f.Status == FriendshipStatus.Accepted);

            if (!areFriends) return ServiceResult<List<GameComparisonDto>>.Failure(CommonErrors.Forbidden());

            var myGames = await _context.UserGames
                .Where(ug => ug.UserId == currentUserId)
                .Select(ug => new { ug.GameId, ug.Game.Title, ug.Game.Genre.Name, CollectionName = ug.Collection.Name })
                .ToListAsync();

            var friendGames = await _context.UserGames
                .Where(ug => ug.UserId == friendId && ug.Collection.IsPublic)
                .Select(ug => new { ug.GameId, ug.Game.Title, ug.Game.Genre.Name, CollectionName = ug.Collection.Name })
                .ToListAsync();

            var allGameIds = myGames.Select(g => g.GameId)
                .Union(friendGames.Select(g => g.GameId))
                .ToList();

            var comparison = allGameIds.Select(id => {
                var myInfo = myGames.FirstOrDefault(g => g.GameId == id);
                var friendInfo = friendGames.FirstOrDefault(g => g.GameId == id);

                var commonInfo = myInfo ?? friendInfo;

                return new GameComparisonDto
                {
                    GameId = id,
                    Title = commonInfo!.Title,
                    GenreName = commonInfo.Name,
                    OwnedByMe = myInfo != null,
                    OwnedByFriend = friendInfo != null,
                    MyCollectionName = myInfo?.CollectionName,
                    FriendCollectionName = friendInfo?.CollectionName
                };
            })
            .OrderBy(g => g.Title)
            .ToList();

            return ServiceResult<List<GameComparisonDto>>.Success(comparison);
        }
    }
}
