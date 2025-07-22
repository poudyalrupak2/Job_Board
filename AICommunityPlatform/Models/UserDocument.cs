
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Models
{
    public enum DocumentType
    {
        Resume,
        CoverLetter,
        Portfolio,
        Certificate,
        Other
    }

    public class UserDocument
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        public DocumentType Type { get; set; }

        public long FileSize { get; set; }

        public string ContentType { get; set; }

        public DateTime UploadedAt { get; set; }

        public bool IsDefault { get; set; }

        // Foreign key
        public int UserId { get; set; }
        public virtual User User { get; set; }
    }
}
