using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IHistoryLogService
    {
        Task<ServiceResult<DataTableResponse<HistoryLogDto>>> GetHistoryLogs(DataTableRequest request);
    }
}
