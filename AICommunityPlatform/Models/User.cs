using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.Models
{
    public enum UserRole
    {
        CommunityMember,
        CommunityGroup,
        Organization,
        SystemAdmin
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string DisplayName => $"{FirstName} {LastName}";

        public UserRole Role { get; set; }

        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public bool IsVerified => IsEmailVerified && IsPhoneVerified;

        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        // Navigation properties
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public virtual ICollection<Connection> SentConnections { get; set; } = new List<Connection>();
        public virtual ICollection<Connection> ReceivedConnections { get; set; } = new List<Connection>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public virtual ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();

    }
}
