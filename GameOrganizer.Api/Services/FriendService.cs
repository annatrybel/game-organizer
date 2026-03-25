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
    }
}
