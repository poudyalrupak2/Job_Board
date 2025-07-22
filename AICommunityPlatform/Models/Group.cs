using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Models
{

    public class Group
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public int ModeratorId { get; set; }
        public virtual User Moderator { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsPrivate { get; set; } = false;

        // Navigation properties
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
