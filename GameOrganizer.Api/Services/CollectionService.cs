using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.View;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace GameOrganizer.Api.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly GameOrganizerDbContext _context;
        private const string ObjectName = "Collection";

        public CollectionService(GameOrganizerDbContext context)
        {
            _context = context;
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

                var groupedData = flatResults
                    .GroupBy(v => new { v.CollectionId, v.CollectionName })
                    .Select(g => new CollectionWithGamesDto
                    {
                        CollectionId = g.Key.CollectionId,
                        CollectionName = g.Key.CollectionName,
                        Games = g.Select(v => new UserGameDto
                        {
                            GameId = v.GameId,
                            Title = v.Title,
                            GenreName = v.GenreName,
                            PlatformName = v.PlatformName,
                            AddedAt = v.AddedAt
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
    }
}

