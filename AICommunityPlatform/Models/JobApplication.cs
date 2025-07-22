namespace AICommunityPlatform.Models
{
    public enum ApplicationStatus
    {
        Applied,
        Viewed,
        Interview,
        Rejected,
        Accepted,
        Withdrawn
    }

    public class JobApplication
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        public virtual Job Job { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public string CoverLetter { get; set; }

        public string Resume { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        public DateTime AppliedDate { get; set; }

        public DateTime? InterviewDate { get; set; }

        public string? Notes { get; set; }
    }
}
