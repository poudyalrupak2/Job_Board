using AICommunityPlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.ViewModels
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public int ConnectionsCount { get; set; }
        public int ApplicationsCount { get; set; }
        public int DocumentsCount { get; set; }
        public List<JobApplication> RecentApplications { get; set; } = new();
    }

    public class EditProfileViewModel
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [StringLength(1000)]
        public string? Skills { get; set; }

        [StringLength(2000)]
        public string? Experience { get; set; }

        public string? ProfilePicture { get; set; }

        public IFormFile? ProfilePictureFile { get; set; }
    }

    public class PublicProfileViewModel
    {
        public User User { get; set; }
        public bool IsOwnProfile { get; set; }
        public ConnectionStatus? ConnectionStatus { get; set; }
        public int? ConnectionId { get; set; }
        public List<Post> RecentPosts { get; set; } = new();
    }
}