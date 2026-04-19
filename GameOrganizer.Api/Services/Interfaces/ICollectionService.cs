using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;
using GameOrganizer.Api.Models.DatabaseModels;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface ICollectionService
    {
        Task<ServiceResult> InitDefaultCollectionsAsync(string userId);
        Task<ServiceResult<DataTableResponse<CollectionWithGamesDto>>> GetMyGamesAsync(string userId, DataTableRequest request);
        Task<ServiceResult<Collection>> CreateCollectionAsync(CollectionDto dto, string userId);
        Task<ServiceResult> UpdateCollectionAsync(CollectionDto dto, string userId);
        Task<ServiceResult> DeleteCollectionAsync(int id, string userId);
        Task<ServiceResult<List<CollectionDto>>> GetUserCollectionsLookupAsync(string userId);
        Task<ServiceResult<SharedCollectionDto>> GetSharedCollectionAsync(Guid shareCode);
    }
}
