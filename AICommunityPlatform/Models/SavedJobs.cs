namespace AICommunityPlatform.Models
{
    public class SavedJob
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        public virtual Job Job { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public DateTime SavedDate { get; set; }

        public DateTime? ReminderDate { get; set; }

        public bool IsReminded { get; set; } = false;
    }
}
