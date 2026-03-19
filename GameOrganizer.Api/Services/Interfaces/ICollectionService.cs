using GameOrganizer.Api.Services.Results;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface ICollectionService
    {
        Task<ServiceResult> InitDefaultCollectionsAsync(string userId);
    }
}
