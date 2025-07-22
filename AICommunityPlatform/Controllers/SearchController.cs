// Controllers/SearchController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using AICommunityPlatform.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Global(string q, string type = "all")
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return View("Index");
            }

            var searchResults = new GlobalSearchViewModel
            {
                Query = q,
                Type = type
            };

            var searchTerm = q.ToLower();

            // Search Jobs
            if (type == "all" || type == "jobs")
            {
                searchResults.Jobs = await _context.Jobs
                    .Include(j => j.Organization)
                    .Where(j => j.IsActive &&
                               (j.Title.ToLower().Contains(searchTerm) ||
                                j.Description.ToLower().Contains(searchTerm) ||
                                j.Requirements.ToLower().Contains(searchTerm) ||
                                j.Organization.DisplayName.ToLower().Contains(searchTerm)))
                    .OrderByDescending(j => j.PostedDate)
                    .Take(type == "jobs" ? 50 : 10)
                    .ToListAsync();
            }

            // Search Members
            if (type == "all" || type == "members")
            {
                searchResults.Members = await _context.Users
                    .Where(u => u.Role == UserRole.CommunityMember &&
                               (u.FirstName.ToLower().Contains(searchTerm) ||
                                u.LastName.ToLower().Contains(searchTerm) ||
                                u.Skills.ToLower().Contains(searchTerm) ||
                                u.Bio.ToLower().Contains(searchTerm) ||
                                u.Experience.ToLower().Contains(searchTerm)))
                    .OrderBy(u => u.FirstName)
                    .Take(type == "members" ? 50 : 10)
                    .ToListAsync();
            }

            // Search Groups
            if (type == "all" || type == "groups")
            {
                searchResults.Groups = await _context.Groups
                    .Include(g => g.Moderator)
                    .Include(g => g.Members)
                    .Where(g => g.Name.ToLower().Contains(searchTerm) ||
                               g.Description.ToLower().Contains(searchTerm))
                    .OrderBy(g => g.Name)
                    .Take(type == "groups" ? 50 : 10)
                    .ToListAsync();
            }

            // Search Posts
            if (type == "all" || type == "posts")
            {
                searchResults.Posts = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .Where(p => p.IsPublic && p.Content.ToLower().Contains(searchTerm))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(type == "posts" ? 50 : 10)
                    .ToListAsync();
            }

            // Search Organizations
            if (type == "all" || type == "organizations")
            {
                searchResults.Organizations = await _context.Users
                    .Where(u => u.Role == UserRole.Organization &&
                               (u.FirstName.ToLower().Contains(searchTerm) ||
                                u.LastName.ToLower().Contains(searchTerm) ||
                                u.Bio.ToLower().Contains(searchTerm)))
                    .OrderBy(u => u.FirstName)
                    .Take(type == "organizations" ? 50 : 10)
                    .ToListAsync();
            }

            return View("Results", searchResults);
        }

        [HttpGet]
        public async Task<IActionResult> Advanced()
        {
            var viewModel = new AdvancedSearchViewModel();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Advanced(AdvancedSearchViewModel model)
        {
            var jobs = _context.Jobs
                .Include(j => j.Organization)
                .Where(j => j.IsActive)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(model.Keywords))
            {
                var keywords = model.Keywords.ToLower();
                jobs = jobs.Where(j => j.Title.ToLower().Contains(keywords) ||
                                      j.Description.ToLower().Contains(keywords) ||
                                      j.Requirements.ToLower().Contains(keywords));
            }

            if (!string.IsNullOrEmpty(model.Location))
            {
                jobs = jobs.Where(j => j.Location.ToLower().Contains(model.Location.ToLower()));
            }

            if (!string.IsNullOrEmpty(model.Company))
            {
                jobs = jobs.Where(j => j.Organization.DisplayName.ToLower().Contains(model.Company.ToLower()));
            }

            if (model.JobType.HasValue)
            {
                jobs = jobs.Where(j => j.Type == model.JobType.Value);
            }

            if (model.IsRemote.HasValue)
            {
                jobs = jobs.Where(j => j.IsRemote == model.IsRemote.Value);
            }

            if (model.MinSalary.HasValue)
            {
                jobs = jobs.Where(j => j.Salary >= model.MinSalary.Value);
            }

            if (model.MaxSalary.HasValue)
            {
                jobs = jobs.Where(j => j.Salary <= model.MaxSalary.Value);
            }

            if (model.PostedSince.HasValue)
            {
                var sinceDate = DateTime.Now.AddDays(-model.PostedSince.Value);
                jobs = jobs.Where(j => j.PostedDate >= sinceDate);
            }

            // Apply sorting
            jobs = model.SortBy switch
            {
                "newest" => jobs.OrderByDescending(j => j.PostedDate),
                "oldest" => jobs.OrderBy(j => j.PostedDate),
                "salary_high" => jobs.OrderByDescending(j => j.Salary),
                "salary_low" => jobs.OrderBy(j => j.Salary),
                "company" => jobs.OrderBy(j => j.Organization.DisplayName),
                _ => jobs.OrderByDescending(j => j.PostedDate)
            };

            model.Results = await jobs.Take(100).ToListAsync();
            model.TotalResults = await jobs.CountAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string term, string type = "all")
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var suggestions = new List<object>();
            var searchTerm = term.ToLower();

            switch (type)
            {
                case "jobs":
                    var jobs = await _context.Jobs
                        .Where(j => j.IsActive && j.Title.ToLower().Contains(searchTerm))
                        .Select(j => new { type = "job", text = j.Title, id = j.Id })
                        .Take(5)
                        .ToListAsync();
                    suggestions.AddRange(jobs);
                    break;

                case "members":
                    var members = await _context.Users
                        .Where(u => u.Role == UserRole.CommunityMember &&
                                   (u.FirstName.ToLower().Contains(searchTerm) ||
                                    u.LastName.ToLower().Contains(searchTerm)))
                        .Select(u => new { type = "member", text = u.DisplayName, id = u.Id })
                        .Take(5)
                        .ToListAsync();
                    suggestions.AddRange(members);
                    break;

                case "skills":
                    var skills =  _context.Users
                        .Where(u => !string.IsNullOrEmpty(u.Skills) && u.Skills.ToLowerInvariant().Contains(searchTerm)).AsEnumerable()
                        .SelectMany(u => u.Skills.Split(','))
                        .Where(s => s.Trim().ToLowerInvariant().Contains(searchTerm.ToLowerInvariant()))
                        .Select(s => new { type = "skill", text = s.Trim(), id = s.Trim() })
                        .Distinct()
                        .Take(5)
                        .ToList();
                    suggestions.AddRange(skills);
                    break;

                case "companies":
                    var companies = await _context.Users
                        .Where(u => u.Role == UserRole.Organization &&
                                   u.DisplayName.ToLower().Contains(searchTerm))
                        .Select(u => new { type = "company", text = u.DisplayName, id = u.Id })
                        .Take(5)
                        .ToListAsync();
                    suggestions.AddRange(companies);
                    break;

                default:
                    // Mixed suggestions for global search
                    var jobSuggestions = await _context.Jobs
                        .Where(j => j.IsActive && j.Title.ToLower().Contains(searchTerm))
                        .Select(j => new { type = "job", text = j.Title, id = j.Id })
                        .Take(3)
                        .ToListAsync();
                    suggestions.AddRange(jobSuggestions);

                    var memberSuggestions = await _context.Users
                        .Where(u => u.Role == UserRole.CommunityMember &&
                                   (u.FirstName.ToLower().Contains(searchTerm) ||
                                    u.LastName.ToLower().Contains(searchTerm)))
                        .Select(u => new { type = "member", text = u.DisplayName, id = u.Id })
                        .Take(3)
                        .ToListAsync();
                    suggestions.AddRange(memberSuggestions);
                    break;
            }

            return Json(suggestions);
        }
    }
}

