// Controllers/AdminController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using AICommunityPlatform.ViewModels;
using System.Security.Claims;
using System.Text;

namespace AICommunityPlatform.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalJobs = await _context.Jobs.CountAsync(),
                TotalApplications = await _context.JobApplications.CountAsync(),
                TotalGroups = await _context.Groups.CountAsync(),
                TotalPosts = await _context.Posts.CountAsync(),
                ActiveJobs = await _context.Jobs.CountAsync(j => j.IsActive),
                PendingApplications = await _context.JobApplications.CountAsync(ja => ja.Status == ApplicationStatus.Applied),

                // Recent activity
                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .ToListAsync(),

                RecentJobs = await _context.Jobs
                    .Include(j => j.Organization)
                    .OrderByDescending(j => j.PostedDate)
                    .Take(10)
                    .ToListAsync(),

                RecentApplications = await _context.JobApplications
                    .Include(ja => ja.Job)
                    .Include(ja => ja.User)
                    .OrderByDescending(ja => ja.AppliedDate)
                    .Take(10)
                    .ToListAsync(),

                // User statistics by role
                UsersByRole = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count),

                // Jobs by type
                JobsByType = await _context.Jobs
                    .GroupBy(j => j.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count),

                // Applications by status
                ApplicationsByStatus = await _context.JobApplications
                    .GroupBy(ja => ja.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count)
            };

            return View(viewModel);
        }

        // User Management
        public async Task<IActionResult> Users(string search, UserRole? role, int page = 1)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.FirstName.Contains(search) ||
                                        u.LastName.Contains(search) ||
                                        u.Email.Contains(search));
            }

            if (role.HasValue)
            {
                users = users.Where(u => u.Role == role.Value);
            }

            var pageSize = 20;
            var totalUsers = await users.CountAsync();
            var userList = await users
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminUsersViewModel
            {
                Users = userList,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
                SearchTerm = search,
                SelectedRole = role
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Toggle active status (you'll need to add IsActive property to User model)
            // user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User status updated" });
        }

        // Job Management
        public async Task<IActionResult> Jobs(string search, JobType? type, bool? isActive, int page = 1)
        {
            var jobs = _context.Jobs.Include(j => j.Organization).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                jobs = jobs.Where(j => j.Title.Contains(search) ||
                                      j.Description.Contains(search) ||
                                      j.Organization.DisplayName.Contains(search));
            }

            if (type.HasValue)
            {
                jobs = jobs.Where(j => j.Type == type.Value);
            }

            if (isActive.HasValue)
            {
                jobs = jobs.Where(j => j.IsActive == isActive.Value);
            }

            var pageSize = 20;
            var totalJobs = await jobs.CountAsync();
            var jobList = await jobs
                .OrderByDescending(j => j.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminJobsViewModel
            {
                Jobs = jobList,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalJobs / (double)pageSize),
                SearchTerm = search,
                SelectedType = type,
                IsActive = isActive
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleJobStatus(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
            {
                return Json(new { success = false, message = "Job not found" });
            }

            job.IsActive = !job.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Job status updated", isActive = job.IsActive });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteJob(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
            {
                return Json(new { success = false, message = "Job not found" });
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Job deleted successfully" });
        }

        // Application Management
        [HttpGet]
        public async Task<IActionResult> Applications(ApplicationStatus? status, int? jobId, int page = 1)
        {
            try
            {
                var applications = _context.JobApplications
                    .Include(ja => ja.Job)
                    .ThenInclude(j => j.Organization)
                    .Include(ja => ja.User)
                    .AsQueryable();

                // Filter by job if jobId is provided
                if (jobId.HasValue)
                {
                    applications = applications.Where(ja => ja.JobId == jobId.Value);

                    // Get job details for display
                    var job = await _context.Jobs
                        .Include(j => j.Organization)
                        .FirstOrDefaultAsync(j => j.Id == jobId.Value);

                    if (job == null)
                    {
                        TempData["Error"] = "Job not found";
                        return RedirectToAction("Jobs");
                    }

                    ViewBag.Job = job;
                }

                // Filter by status if provided
                if (status.HasValue)
                {
                    applications = applications.Where(ja => ja.Status == status.Value);
                }

                var pageSize = 20;
                var totalApplications = await applications.CountAsync();
                var applicationList = await applications
                    .OrderByDescending(ja => ja.AppliedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var viewModel = new AdminApplicationsViewModel
                {
                    Applications = applicationList,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalApplications / (double)pageSize),
                    SelectedStatus = status,
                    JobId = jobId  // Add this property to your viewmodel
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading applications";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, ApplicationStatus newStatus)
        {
            try
            {
                var application = await _context.JobApplications
                    .Include(ja => ja.User)
                    .Include(ja => ja.Job)
                    .FirstOrDefaultAsync(ja => ja.Id == applicationId);

                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found" });
                }

                application.Status = newStatus;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Application status updated to {newStatus}",
                    newStatus = newStatus.ToString()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating application status" });
            }
        }



        // Group Management
        public async Task<IActionResult> Groups(string search, int page = 1)
        {
            var groups = _context.Groups
                .Include(g => g.Moderator)
                .Include(g => g.Members)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                groups = groups.Where(g => g.Name.Contains(search) ||
                                          g.Description.Contains(search));
            }

            var pageSize = 20;
            var totalGroups = await groups.CountAsync();
            var groupList = await groups
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminGroupsViewModel
            {
                Groups = groupList,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalGroups / (double)pageSize),
                SearchTerm = search
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                return Json(new { success = false, message = "Group not found" });
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Group deleted successfully" });
        }

        // Reports
        public async Task<IActionResult> Reports()
        {
            var viewModel = new AdminReportsViewModel
            {
                UserGrowth = await GetUserGrowthData(),
                JobPostings = await GetJobPostingData(),
                ApplicationTrends = await GetApplicationTrendsData(),
                PopularSkills = await GetPopularSkillsData()
            };

            return View(viewModel);
        }

        private async Task<List<ChartDataPoint>> GetUserGrowthData()
        {
            var last12Months = Enumerable.Range(0, 12)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var data = new List<ChartDataPoint>();

            foreach (var month in last12Months)
            {
                var count = await _context.Users
                    .CountAsync(u => u.CreatedAt.Year == month.Year && u.CreatedAt.Month == month.Month);

                data.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = count
                });
            }

            return data;
        }

        private async Task<List<ChartDataPoint>> GetJobPostingData()
        {
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var data = new List<ChartDataPoint>();

            foreach (var month in last6Months)
            {
                var count = await _context.Jobs
                    .CountAsync(j => j.PostedDate.Year == month.Year && j.PostedDate.Month == month.Month);

                data.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = count
                });
            }

            return data;
        }

        private async Task<List<ChartDataPoint>> GetApplicationTrendsData()
        {
            var statusCounts = await _context.JobApplications
                .GroupBy(ja => ja.Status)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString(),
                    Value = g.Count()
                })
                .ToListAsync();

            return statusCounts;
        }

        private async Task<List<ChartDataPoint>> GetPopularSkillsData()
        {
            var skillCounts =  _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Skills)).AsEnumerable()
                .SelectMany(u => u.Skills.Split(','))
                .GroupBy(s => s.Trim().ToLower())
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(s => s.Value)
                .Take(10)
                .ToList();

            return skillCounts;
        }

        // System Settings
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(AdminSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Update system settings (you'll need to implement a settings system)
                TempData["Success"] = "Settings updated successfully";
                return RedirectToAction("Settings");
            }

            return View("Settings", model);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
        [HttpGet]
        public async Task<IActionResult> CreateJob()
        {
            var organizations =  _context.Users
                .Where(u => u.Role == UserRole.Organization).AsEnumerable()
                .OrderBy(u => u.DisplayName)
                .ToList();

            var viewModel = new AdminCreateJobViewModel
            {
                Organizations = organizations,
                IsActive = true
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(AdminCreateJobViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var job = new Job
                    {
                        Title = model.Title,
                        Description = model.Description,
                        Requirements = model.Requirements,
                        Type = model.Type,
                        Location = model.Location,
                        Salary = model.Salary,
                        IsRemote = model.IsRemote,
                        PostedDate = DateTime.Now,
                        Deadline = model.Deadline,
                        IsActive = model.IsActive,
                        OrganizationId = model.OrganizationId
                    };

                    _context.Jobs.Add(job);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Job created successfully!";
                    return RedirectToAction("Details", new { id = job.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the job. Please try again.");
                }
            }

            // Reload organizations if model state is invalid
            model.Organizations = await _context.Users
                .Where(u => u.Role == UserRole.Organization)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Organization)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            var organizations = await _context.Users
                .Where(u => u.Role == UserRole.Organization)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            var viewModel = new AdminEditJobViewModel
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Requirements = job.Requirements,
                Type = job.Type,
                Location = job.Location,
                Salary = job.Salary,
                IsRemote = job.IsRemote,
                Deadline = job.Deadline,
                IsActive = job.IsActive,
                OrganizationId = job.OrganizationId,
                PostedDate = job.PostedDate,
                ApplicationCount = job.Applications.Count,
                Organizations = organizations
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(AdminEditJobViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var job = await _context.Jobs.FindAsync(model.Id);
                    if (job == null)
                    {
                        return NotFound();
                    }

                    job.Title = model.Title;
                    job.Description = model.Description;
                    job.Requirements = model.Requirements;
                    job.Type = model.Type;
                    job.Location = model.Location;
                    job.Salary = model.Salary;
                    job.IsRemote = model.IsRemote;
                    job.Deadline = model.Deadline;
                    job.IsActive = model.IsActive;
                    job.OrganizationId = model.OrganizationId;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Job updated successfully!";
                    return RedirectToAction("Details", new { id = job.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the job. Please try again.");
                }
            }

            // Reload organizations if model state is invalid
            model.Organizations = await _context.Users
                .Where(u => u.Role == UserRole.Organization)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuplicateJob(int jobId)
        {
            try
            {
                var originalJob = await _context.Jobs.FindAsync(jobId);
                if (originalJob == null)
                {
                    return Json(new { success = false, message = "Job not found" });
                }

                var duplicatedJob = new Job
                {
                    Title = originalJob.Title + " (Copy)",
                    Description = originalJob.Description,
                    Requirements = originalJob.Requirements,
                    Type = originalJob.Type,
                    Location = originalJob.Location,
                    Salary = originalJob.Salary,
                    IsRemote = originalJob.IsRemote,
                    PostedDate = DateTime.Now,
                    Deadline = originalJob.Deadline,
                    IsActive = false, // Start as inactive
                    OrganizationId = originalJob.OrganizationId
                };

                _context.Jobs.Add(duplicatedJob);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Job duplicated successfully!", newJobId = duplicatedJob.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error duplicating job" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.Organization)
                    .Include(j => j.Applications)
                    .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    TempData["Error"] = "Job not found";
                    return RedirectToAction("Jobs");
                }

                // Get application statistics grouped by status
                var applicationsByStatus = job.Applications
                    .GroupBy(a => a.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate total views (you might want to implement a view tracking system)
                var totalViews = 0; // Placeholder - implement view tracking if needed

                var viewModel = new AdminJobDetailsViewModel
                {
                    Job = job,
                    Applications = job.Applications.OrderByDescending(a => a.AppliedDate).ToList(),
                    TotalApplications = job.Applications.Count,
                    TotalViews = totalViews,
                    ApplicationsByStatus = applicationsByStatus
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading job details";
                return RedirectToAction("Jobs");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ExportApplications(int jobId)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.Applications)
                    .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(j => j.Id == jobId);

                if (job == null)
                {
                    return NotFound();
                }

                var csv = new StringBuilder();
                csv.AppendLine("Application ID,Job Title,Applicant Name,Email,Phone,Applied Date,Status,Cover Letter,Resume");

                foreach (var application in job.Applications)
                {
                    csv.AppendLine($"{application.Id}," +
                                  $"\"{job.Title}\"," +
                                  $"\"{application.User.DisplayName}\"," +
                                  $"\"{application.User.Email}\"," +
                                  $"\"{application.User.Phone ?? "N/A"}\"," +
                                  $"{application.AppliedDate:yyyy-MM-dd}," +
                                  $"{application.Status}," +
                                  $"\"{application.CoverLetter?.Replace("\"", "\"\"") ?? "N/A"}\"," +
                                  $"\"{application.Resume?.Replace("\"", "\"\"") ?? "N/A"}\"");
                }

                var fileName = $"applications_{job.Title}_{DateTime.Now:yyyyMMdd}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error exporting applications";
                return RedirectToAction("Details", new { id = jobId });
            }
        }
    }
}

