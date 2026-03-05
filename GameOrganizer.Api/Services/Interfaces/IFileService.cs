namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IFileService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        // Task DeleteImageAsync(string publicId); 
    }
}
