using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Models.Dto.GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IStatsService
    {
        Task<ServiceResult<UserLibraryStatsDto>> GetMyLibraryStatsAsync(string userId);
        Task<ServiceResult<GlobalStatsDto>> GetGlobalStatsAsync();
    }
}
