using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace GameOrganizer.Api.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly GameOrganizerDbContext _context;

        public CollectionService(GameOrganizerDbContext context)
        {
            _context = context;
        }
        public async Task<ServiceResult> InitDefaultCollectionsAsync(string userId)
        {
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

        public async Task<ServiceResult<IEnumerable<Collection>>> GetMyCollectionsAsync(string userId)
        {
            var collections = await _context.Collections
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return ServiceResult<IEnumerable<Collection>>.Success(collections);
        }
    }
}
