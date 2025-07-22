using Microsoft.AspNetCore.Builder;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Models
{
    public enum JobType
    {
        FullTime,
        PartTime,
        Contract,
        Internship,
        Volunteer,
        Project
    }

    public class Job
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public string Requirements { get; set; }

        public JobType Type { get; set; }

        public string Location { get; set; }

        public decimal? Salary { get; set; }

        public bool IsRemote { get; set; }

        public DateTime PostedDate { get; set; }

        public DateTime? Deadline { get; set; }

        public bool IsActive { get; set; } = true;

        // Foreign key
        public int OrganizationId { get; set; }
        public virtual User Organization { get; set; }

        // Navigation properties
        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public virtual ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    }
}



