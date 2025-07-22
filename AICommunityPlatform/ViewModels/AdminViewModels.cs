using AICommunityPlatform.Models;

namespace AICommunityPlatform.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public int TotalGroups { get; set; }
        public int TotalPosts { get; set; }
        public int ActiveJobs { get; set; }
        public int PendingApplications { get; set; }

        public List<User> RecentUsers { get; set; } = new();
        public List<Job> RecentJobs { get; set; } = new();
        public List<JobApplication> RecentApplications { get; set; } = new();

        public Dictionary<UserRole, int> UsersByRole { get; set; } = new();
        public Dictionary<JobType, int> JobsByType { get; set; } = new();
        public Dictionary<ApplicationStatus, int> ApplicationsByStatus { get; set; } = new();
    }

    public class AdminUsersViewModel
    {
        public List<User> Users { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public UserRole? SelectedRole { get; set; }
    }

    public class AdminJobsViewModel
    {
        public List<Job> Jobs { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public JobType? SelectedType { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AdminApplicationsViewModel
    {
        public List<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public ApplicationStatus? SelectedStatus { get; set; }
        public int? JobId { get; set; } // ADD THIS LINE

        // Helper properties
        public int TotalApplications => Applications?.Count ?? 0;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminGroupsViewModel
    {
        public List<Group> Groups { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class AdminReportsViewModel
    {
        public List<ChartDataPoint> UserGrowth { get; set; } = new();
        public List<ChartDataPoint> JobPostings { get; set; } = new();
        public List<ChartDataPoint> ApplicationTrends { get; set; } = new();
        public List<ChartDataPoint> PopularSkills { get; set; } = new();
    }

    public class AdminSettingsViewModel
    {
        public string SiteName { get; set; } = "AI Community Platform";
        public string SiteDescription { get; set; } = "";
        public bool AllowRegistration { get; set; } = true;
        public bool RequireEmailVerification { get; set; } = true;
        public int MaxFileUploadSize { get; set; } = 10; // MB
        public string[] AllowedFileTypes { get; set; } = { ".pdf", ".doc", ".docx" };
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
    }
}