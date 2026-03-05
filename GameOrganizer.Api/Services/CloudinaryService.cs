using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GameOrganizer.Api.Services.Interfaces;

namespace GameOrganizer.Api.Services
{
    public class CloudinaryService : IFileService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var acc = new Account(
                config["CLOUDINARY_CLOUD_NAME"],
                config["CLOUDINARY_API_KEY"],
                config["CLOUDINARY_API_SECRET"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file.Length <= 0) return null;

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                // transformacja (np. kwadrat 500x500)
                //Transformation = new Transformation().Height(500).Width(500).Crop("fill")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception(uploadResult.Error.Message);

            return uploadResult.SecureUrl.ToString();
        }
    }
}
