
using AICommunityPlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.ViewModels
{
    public class GlobalSearchViewModel
    {
        public string Query { get; set; } = "";
        public string Type { get; set; } = "all";

        public List<Job> Jobs { get; set; } = new();
        public List<User> Members { get; set; } = new();
        public List<Group> Groups { get; set; } = new();
        public List<Post> Posts { get; set; } = new();
        public List<User> Organizations { get; set; } = new();

        public int TotalResults => Jobs.Count + Members.Count + Groups.Count + Posts.Count + Organizations.Count;
    }

    public class AdvancedSearchViewModel
    {
        [Display(Name = "Keywords")]
        public string? Keywords { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Company")]
        public string? Company { get; set; }

        [Display(Name = "Job Type")]
        public JobType? JobType { get; set; }

        [Display(Name = "Remote Work")]
        public bool? IsRemote { get; set; }

        [Display(Name = "Minimum Salary")]
        public decimal? MinSalary { get; set; }

        [Display(Name = "Maximum Salary")]
        public decimal? MaxSalary { get; set; }

        [Display(Name = "Posted Since (days)")]
        public int? PostedSince { get; set; }

        [Display(Name = "Sort By")]
        public string SortBy { get; set; } = "newest";

        public List<Job> Results { get; set; } = new();
        public int TotalResults { get; set; }
    }
}