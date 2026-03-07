namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IFileService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string? imageUrl);
    }
}
