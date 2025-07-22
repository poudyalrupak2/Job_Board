using AICommunityPlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.ViewModels
{
    

    public class AdminJobDetailsViewModel
    {
        public Job Job { get; set; } = null!;
        public List<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public Dictionary<ApplicationStatus, int> ApplicationsByStatus { get; set; } = new Dictionary<ApplicationStatus, int>();
        public int TotalViews { get; set; }
        public int TotalApplications { get; set; }
    }

    public class AdminCreateJobViewModel
    {
        [Required(ErrorMessage = "Job title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job description is required")]
        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Requirements are required")]
        [StringLength(3000, ErrorMessage = "Requirements cannot exceed 3000 characters")]
        public string Requirements { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job type is required")]
        public JobType Type { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
        public decimal? Salary { get; set; }

        public bool IsRemote { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Organization is required")]
        public int OrganizationId { get; set; }

        public List<User> Organizations { get; set; } = new List<User>();
    }

    public class AdminEditJobViewModel : AdminCreateJobViewModel
    {
        public int Id { get; set; }
        public DateTime PostedDate { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class JobFiltersViewModel
    {
        public string? SearchTerm { get; set; }
        public JobType? Type { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsRemote { get; set; }
        public DateTime? PostedAfter { get; set; }
        public DateTime? PostedBefore { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public int? OrganizationId { get; set; }
    }
}