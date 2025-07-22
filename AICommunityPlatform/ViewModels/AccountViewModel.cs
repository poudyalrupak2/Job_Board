using System.ComponentModel.DataAnnotations;
using AICommunityPlatform.Models;



    namespace AICommunityPlatform.ViewModels
    {
        public class RegisterViewModel
        {
            [Required(ErrorMessage = "First name is required")]
            [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required")]
            [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Please enter a valid phone number")]
            [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
            public string? Phone { get; set; }

            [Required(ErrorMessage = "Please select your role")]
            public UserRole Role { get; set; }

            [StringLength(1000, ErrorMessage = "Skills cannot exceed 1000 characters")]
            public string? Skills { get; set; }

            [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
            public string? Bio { get; set; }
        }

        public class LoginViewModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public class VerifyViewModel
        {
            [Required]
            public int UserId { get; set; }

            [Required(ErrorMessage = "Email verification code is required")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
            [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
            public string EmailCode { get; set; } = string.Empty;
        }
    

   
}
