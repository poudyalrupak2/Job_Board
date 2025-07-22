using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public int AuthorId { get; set; }
        public virtual User Author { get; set; }

        public int? GroupId { get; set; }
        public virtual Group Group { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsPublic { get; set; } = true;

        // Navigation properties
        public virtual ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
        public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    }
}
