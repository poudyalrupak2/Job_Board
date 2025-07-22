using AICommunityPlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace AICommunityPlatform.ViewModels
{
    public class DocumentUploadViewModel
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public DocumentType Type { get; set; }

        public string Description { get; set; }
    }
}
