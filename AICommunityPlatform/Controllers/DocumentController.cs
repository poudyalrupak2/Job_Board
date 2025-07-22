using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AICommunityPlatform.Services;
using AICommunityPlatform.Models;
using AICommunityPlatform.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AICommunityPlatform.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".txt" };
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
        public readonly IWebHostEnvironment _environment;
        public DocumentController(ApplicationDbContext context, IFileUploadService fileUploadService, IWebHostEnvironment environment)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var documents = await _context.UserDocuments
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return View(documents);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, DocumentType type)
        {
            try
            {
                if (!_fileUploadService.IsValidFile(file, _allowedExtensions, _maxFileSize))
                {
                    return Json(new { success = false, message = "Invalid file. Please upload PDF, DOC, or DOCX files under 10MB." });
                }

                var userId = GetCurrentUserId();
                var filePath = await _fileUploadService.UploadFileAsync(file, "documents", userId.ToString());

                // Save document record
                var document = new UserDocument
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    Type = type,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    UploadedAt = DateTime.Now,
                    UserId = userId
                };

                _context.UserDocuments.Add(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, documentId = document.Id, message = "File uploaded successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error uploading file. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _context.UserDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

                if (document == null)
                {
                    return Json(new { success = false, message = "Document not found." });
                }

                // Delete file from disk
                await _fileUploadService.DeleteFileAsync(document.FilePath);

                // Delete from database
                _context.UserDocuments.Remove(document);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Document deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting document." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Remove default from all documents
                var userDocuments = await _context.UserDocuments
                    .Where(d => d.UserId == userId)
                    .ToListAsync();

                foreach (var doc in userDocuments)
                {
                    doc.IsDefault = false;
                }

                // Set new default
                var document = userDocuments.FirstOrDefault(d => d.Id == id);
                if (document != null)
                {
                    document.IsDefault = true;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Default document updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating default document." });
            }
        }

        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var document = await _context.UserDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

                if (document == null)
                {
                    return NotFound();
                }

                var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, document.ContentType, document.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Error downloading file.");
            }
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}
