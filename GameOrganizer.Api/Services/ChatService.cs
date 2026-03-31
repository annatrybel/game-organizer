using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameOrganizer.Api.Services
{
    public class ChatService : IChatService
    {
        private readonly GameOrganizerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatService(GameOrganizerDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResult<IEnumerable<ChatGroupDto>>> GetUserChatsAsync(string userId)
        {
            var chats = await _context.ChatGroups
                .Where(g => g.Members.Any(m => m.UserId == userId))
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Include(g => g.Messages)
                .OrderByDescending(g => g.Messages.Max(m => (DateTime?)m.Timestamp) ?? g.CreatedAt)
                .Select(g => new ChatGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    LastMessage = g.Messages.OrderByDescending(m => m.Timestamp).Select(m => m.Content).FirstOrDefault() ?? "Brak wiadomości",
                    LastMessageTime = g.Messages.OrderByDescending(m => m.Timestamp).Select(m => (DateTime?)m.Timestamp).FirstOrDefault(),
                    Participants = g.Members.Where(m => m.UserId != userId).Select(m => m.User.UserName!).ToList()
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<ChatGroupDto>>.Success(chats);
        }

        public async Task<ServiceResult<IEnumerable<ChatMessageDto>>> GetChatHistoryAsync(int groupId, string userId)
        {
            var isMember = await _context.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == groupId && m.UserId == userId);
            if (!isMember) return ServiceResult<IEnumerable<ChatMessageDto>>.Failure(CommonErrors.Forbidden());

            var history = await _context.ChatMessages
                .Where(m => m.GroupId == groupId)
                .Include(m => m.Sender) 
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.UserName ?? "Nieznany", 
                    GroupId = m.GroupId
                })
                .Take(100)
                .ToListAsync();

            return ServiceResult<IEnumerable<ChatMessageDto>>.Success(history);
        }


        public async Task<ServiceResult<int>> CreateChatAsync(string currentUserId, CreateChatRequest request)
        {
            if (request.UserIds.Count == 1)
            {
                var targetId = request.UserIds.First();
                var existing = await _context.ChatGroups
                    .Where(g => g.Members.Count == 2 &&
                                g.Members.Any(m => m.UserId == currentUserId) &&
                                g.Members.Any(m => m.UserId == targetId))
                    .Select(g => g.Id)
                    .FirstOrDefaultAsync();

                if (existing != 0) return ServiceResult<int>.Success(existing);
            }

            var newGroup = new ChatGroup { Name = request.GroupName };
            _context.ChatGroups.Add(newGroup);

            var members = request.UserIds.Select(uid => new ChatGroupMember { ChatGroup = newGroup, UserId = uid }).ToList();
            members.Add(new ChatGroupMember { ChatGroup = newGroup, UserId = currentUserId });

            _context.ChatGroupMembers.AddRange(members);
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(newGroup.Id);
        }

        public async Task<ChatMessage> SaveMessageAsync(string senderId, string content, int groupId)
        {
            var message = new ChatMessage
            {
                SenderId = senderId,
                Content = content,
                GroupId = groupId,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }


        public async Task<ServiceResult> AddUserToGroupAsync(int groupId, string requesterId, string targetUserId)
        {
            var isMember = await _context.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == groupId && m.UserId == requesterId);
            if (!isMember) return ServiceResult.Failure(CommonErrors.Forbidden());

            var alreadyIn = await _context.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == groupId && m.UserId == targetUserId);
            if (alreadyIn) return ServiceResult.Success();

            _context.ChatGroupMembers.Add(new ChatGroupMember { ChatGroupId = groupId, UserId = targetUserId });
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveUserFromGroupAsync(int groupId, string userId)
        {
            var member = await _context.ChatGroupMembers
                .FirstOrDefaultAsync(m => m.ChatGroupId == groupId && m.UserId == userId);

            if (member == null)
                return ServiceResult.Failure(CommonErrors.NotFound("Członek czatu", userId));

            _context.ChatGroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            var remainingMembers = await _context.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == groupId);

            if (!remainingMembers)
            {
                var group = await _context.ChatGroups.FindAsync(groupId);
                if (group != null)
                {
                    _context.ChatGroups.Remove(group);
                    await _context.SaveChangesAsync();
                }
            }

            return ServiceResult.Success();
        }
    }
}