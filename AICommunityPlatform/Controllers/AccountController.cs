using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AICommunityPlatform.Data;
using AICommunityPlatform.Models;
using AICommunityPlatform.ViewModels;
using AICommunityPlatform.Services;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Net;

namespace AICommunityPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;

        public AccountController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService)
        {
            _context = context;
            _emailService = emailService;
            _passwordService = passwordService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    return View(model);
                }

                // Check if phone already exists (only if phone is provided)
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    var existingPhone = await _context.Users
                        .FirstOrDefaultAsync(u => u.Phone == model.Phone);

                    if (existingPhone != null)
                    {
                        ModelState.AddModelError("Phone", "Phone number already exists");
                        return View(model);
                    }
                }

                // Hash the password
                var hashedPassword = _passwordService.HashPassword(model.Password);

                // Create new user
                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = hashedPassword,
                    Phone = model.Phone,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role,
                    Skills = model.Skills,
                    Bio = model.Bio,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsEmailVerified = false,
                    IsPhoneVerified = string.IsNullOrEmpty(model.Phone) // True if no phone provided
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send email verification
                await SendEmailVerification(user);

                TempData["UserId"] = user.Id;
                TempData["Message"] = "Registration successful! Please check your email for verification code.";

                return RedirectToAction("Verify");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && _passwordService.VerifyPassword(model.Password, user.PasswordHash))
                {
                    // Check if user is verified (email only)
                    if (!user.IsEmailVerified)
                    {
                        TempData["Error"] = "Please verify your email address before logging in.";
                        TempData["UserId"] = user.Id;
                        return RedirectToAction("Verify");
                    }

                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.DisplayName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync("CookieAuth", claimsPrincipal, authProperties);

                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    TempData["Error"] = "Invalid email or password. Please try again.";
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Verify()
        {
            if (TempData["UserId"] != null)
            {
                ViewBag.UserId = TempData["UserId"];
                TempData.Keep("UserId"); // Keep for potential resend
                return View();
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Verify(VerifyViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);

                if (user != null)
                {
                    // In a real application, you'd verify the code against stored value
                    // For now, we'll assume any 6-digit code is valid
                    if (model.EmailCode.Length == 6 && model.EmailCode.All(char.IsDigit))
                    {
                        user.IsEmailVerified = true;
                        user.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();

                        TempData["Success"] = "Email verification successful! You can now log in.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("EmailCode", "Invalid verification code. Please enter a 6-digit code.");
                        ViewBag.UserId = model.UserId;
                    }
                }
                else
                {
                    ModelState.AddModelError("", "User not found");
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendVerification(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                await SendEmailVerification(user);
                TempData["Success"] = "Verification code resent to your email!";
                TempData["UserId"] = userId;
            }
            else
            {
                TempData["Error"] = "User not found";
            }

            return RedirectToAction("Verify");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email address");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                // In a real application, you'd generate a password reset token
                // and send it via email
                await _emailService.SendEmailAsync(user.Email, "Password Reset Request",
                    "If you requested a password reset, please contact support. This feature is coming soon.");
            }

            TempData["Success"] = "If an account with that email exists, we've sent password reset instructions.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        private async Task SendEmailVerification(User user)
        {
            // Generate random 6-digit code
            var emailCode = new Random().Next(100000, 999999).ToString();

            // In a real application, you'd store this code in the database with expiration
            // For demo purposes, we're just sending it via email

            var subject = "Email Verification Code - AI Community Platform";
            var body = $@"
                <h2>Welcome to AI Community Platform!</h2>
                <p>Thank you for registering. Your email verification code is:</p>
                <h1 style='color: #4F46E5; font-size: 36px; letter-spacing: 5px;'>{emailCode}</h1>
                <p>This code is valid for 15 minutes.</p>
                <p>If you didn't request this code, please ignore this email.</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body);
        }
    }
}