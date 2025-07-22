// Services/IFileUploadService.cs
namespace AICommunityPlatform.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName, string userId);
        Task<bool> DeleteFileAsync(string filePath);
        bool IsValidFile(IFormFile file, string[] allowedExtensions, long maxSizeInBytes);
        string GetFileUrl(string filePath);
    }
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName, string userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");

                // Create unique filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", folderName);

                // Ensure directory exists
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database storage
                return $"/uploads/{folderName}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public bool IsValidFile(IFormFile file, string[] allowedExtensions, long maxSizeInBytes)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size
            if (file.Length > maxSizeInBytes)
                return false;

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        public string GetFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            return filePath.StartsWith("/") ? filePath : $"/{filePath}";
        }
    }
}