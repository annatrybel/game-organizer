using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameOrganizer.Api.Services
{
    public class ChatService : IChatService
    {
        private readonly GameOrganizerDbContext _context;

        public ChatService(GameOrganizerDbContext context)
        {
            _context = context;
        }

        public async Task<ChatMessage> SaveMessageAsync(string senderId, string content, int? groupId, string? receiverId)
        {
            var message = new ChatMessage
            {
                SenderId = senderId,
                Content = content,
                GroupId = groupId,
                ReceiverId = receiverId,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<IEnumerable<ChatMessage>> GetHistoryAsync(int groupId)
        {
            return await _context.ChatMessages
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.Timestamp)
                .Take(50)
                .ToListAsync();
        }
    }
}