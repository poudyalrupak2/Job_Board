using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using System.Security.Claims;

namespace AICommunityPlatform.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm, JobType? jobType, string location)
        {
            var jobs = _context.Jobs
                .Include(j => j.Organization)
                .Where(j => j.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                jobs = jobs.Where(j => j.Title.Contains(searchTerm) ||
                                      j.Description.Contains(searchTerm));
            }

            if (jobType.HasValue)
            {
                jobs = jobs.Where(j => j.Type == jobType.Value);
            }

            if (!string.IsNullOrEmpty(location))
            {
                jobs = jobs.Where(j => j.Location.Contains(location));
            }

            var jobList = await jobs.OrderByDescending(j => j.PostedDate).ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.JobType = jobType;
            ViewBag.Location = location;

            return View(jobList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Organization)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        [HttpPost]
        public async Task<IActionResult> Apply(int jobId, string coverLetter, IFormFile resume)
        {
            var userId = GetCurrentUserId();

            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(ja => ja.JobId == jobId && ja.UserId == userId);

            if (existingApplication != null)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction("Details", new { id = jobId });
            }

            // Handle resume upload
            string resumeFileName = null;
            if (resume != null && resume.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(resume.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Please upload a valid resume file (PDF, DOC, or DOCX).";
                    return RedirectToAction("Details", new { id = jobId });
                }

                // Generate unique filename
                resumeFileName = $"{userId}_{jobId}_{Guid.NewGuid()}{fileExtension}";
                var resumePath = Path.Combine("wwwroot/uploads/resumes", resumeFileName);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(resumePath));

                // Save the file
                using (var stream = new FileStream(resumePath, FileMode.Create))
                {
                    await resume.CopyToAsync(stream);
                }
            }

            var application = new JobApplication
            {
                JobId = jobId,
                UserId = userId,
                CoverLetter = coverLetter,
                Resume = resumeFileName, // Store the filename
                AppliedDate = DateTime.Now,
                Status = ApplicationStatus.Applied
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction("Details", new { id = jobId });
        }


        [HttpPost]
        public async Task<IActionResult> SaveJob(int jobId)
        {
            var userId = GetCurrentUserId();

            var existingSave = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.JobId == jobId && sj.UserId == userId);

            if (existingSave == null)
            {
                var savedJob = new SavedJob
                {
                    JobId = jobId,
                    UserId = userId,
                    SavedDate = DateTime.Now
                };

                _context.SavedJobs.Add(savedJob);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        public async Task<IActionResult> MyApplications()
        {
            var userId = GetCurrentUserId();

            var applications = await _context.JobApplications
                .Include(ja => ja.Job)
                .ThenInclude(j => j.Organization)
                .Where(ja => ja.UserId == userId)
                .OrderByDescending(ja => ja.AppliedDate)
                .ToListAsync();

            return View(applications);
        }

        public async Task<IActionResult> SavedJobs()
        {
            var userId = GetCurrentUserId();

            var savedJobs = await _context.SavedJobs
                .Include(sj => sj.Job)
                .ThenInclude(j => j.Organization)
                .Where(sj => sj.UserId == userId)
                .OrderByDescending(sj => sj.SavedDate)
                .ToListAsync();

            return View(savedJobs);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}
