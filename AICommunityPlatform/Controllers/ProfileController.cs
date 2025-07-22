// Controllers/ProfileController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using AICommunityPlatform.ViewModels;
using AICommunityPlatform.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;

        public ProfileController(ApplicationDbContext context, IFileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users
                .Include(u => u.Documents)
                .Include(u => u.SentConnections)
                .Include(u => u.ReceivedConnections)
                .Include(u => u.JobApplications)
                .ThenInclude(ja => ja.Job)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ProfileViewModel
            {
                User = user,
                ConnectionsCount = user.SentConnections.Count(c => c.Status == ConnectionStatus.Accepted) +
                                  user.ReceivedConnections.Count(c => c.Status == ConnectionStatus.Accepted),
                ApplicationsCount = user.JobApplications.Count(),
                DocumentsCount = user.Documents.Count(),
                RecentApplications = user.JobApplications
                    .OrderByDescending(ja => ja.AppliedDate)
                    .Take(5)
                    .ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Bio = user.Bio,
                Skills = user.Skills,
                Experience = user.Experience,
                ProfilePicture = user.ProfilePicture
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound();
                }

                // Handle profile picture upload
                if (model.ProfilePictureFile != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var maxFileSize = 5 * 1024 * 1024; // 5MB

                    if (_fileUploadService.IsValidFile(model.ProfilePictureFile, allowedExtensions, maxFileSize))
                    {
                        // Delete old profile picture if exists
                        if (!string.IsNullOrEmpty(user.ProfilePicture))
                        {
                            await _fileUploadService.DeleteFileAsync(user.ProfilePicture);
                        }

                        // Upload new profile picture
                        var profilePicturePath = await _fileUploadService.UploadFileAsync(
                            model.ProfilePictureFile, "profile-pictures", userId.ToString());

                        user.ProfilePicture = profilePicturePath;
                    }
                    else
                    {
                        ModelState.AddModelError("ProfilePictureFile", "Invalid image file. Please upload JPG, PNG, or GIF files under 5MB.");
                        return View(model);
                    }
                }

                // Update user information
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Phone = model.Phone;
                user.Bio = model.Bio;
                user.Skills = model.Skills;
                user.Experience = model.Experience;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public async Task<IActionResult> View(int id)
        {
            var user = await _context.Users
                .Include(u => u.Documents)
                .Include(u => u.Posts)
                .ThenInclude(p => p.Likes)
                .Include(u => u.Posts)
                .ThenInclude(p => p.Comments)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            var connection = await _context.Connections
                .FirstOrDefaultAsync(c =>
                    (c.SenderId == currentUserId && c.ReceiverId == id) ||
                    (c.SenderId == id && c.ReceiverId == currentUserId));

            var viewModel = new PublicProfileViewModel
            {
                User = user,
                IsOwnProfile = currentUserId == id,
                ConnectionStatus = connection?.Status,
                ConnectionId = connection?.Id,
                RecentPosts = user.Posts
                    .Where(p => p.IsPublic)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var maxFileSize = 5 * 1024 * 1024; // 5MB

                if (!_fileUploadService.IsValidFile(profilePicture, allowedExtensions, maxFileSize))
                {
                    return Json(new { success = false, message = "Invalid image file. Please upload JPG, PNG, or GIF files under 5MB." });
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    await _fileUploadService.DeleteFileAsync(user.ProfilePicture);
                }

                // Upload new profile picture
                var profilePicturePath = await _fileUploadService.UploadFileAsync(
                    profilePicture, "profile-pictures", userId.ToString());

                user.ProfilePicture = profilePicturePath;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, imageUrl = profilePicturePath, message = "Profile picture updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating profile picture." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    await _fileUploadService.DeleteFileAsync(user.ProfilePicture);
                    user.ProfilePicture = null;
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Profile picture removed successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error removing profile picture." });
            }
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}

