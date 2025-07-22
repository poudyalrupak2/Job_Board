using AICommunityPlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace AICommunityPlatform.Services
{
    public interface INotificationService
    {
        Task SendJobApplicationNotificationAsync(int jobId, int applicantId);
        Task SendConnectionRequestNotificationAsync(int senderId, int receiverId);
        Task SendMessageNotificationAsync(int senderId, int receiverId, string messageContent);
        Task SendJobStatusUpdateAsync(int applicationId, string newStatus);
        Task SendWelcomeEmailAsync(int userId);
        Task SendJobReminderAsync(int savedJobId);
    }



    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, IEmailService emailService, ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendJobApplicationNotificationAsync(int jobId, int applicantId)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.Organization)
                    .FirstOrDefaultAsync(j => j.Id == jobId);

                var applicant = await _context.Users.FindAsync(applicantId);

                if (job != null && applicant != null)
                {
                    var subject = $"New Application for {job.Title}";
                    var body = $@"
                        <h2>New Job Application Received</h2>
                        <p>You have received a new application for the position: <strong>{job.Title}</strong></p>
                        <p><strong>Applicant:</strong> {applicant.DisplayName}</p>
                        <p><strong>Applied:</strong> {DateTime.Now:MMM dd, yyyy}</p>
                        <p>
                            <a href='https://your-domain.com/Jobs/Details/{job.Id}' 
                               style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                               View Application
                            </a>
                        </p>
                    ";

                    await _emailService.SendEmailAsync(job.Organization.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending job application notification");
            }
        }

        public async Task SendConnectionRequestNotificationAsync(int senderId, int receiverId)
        {
            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                var receiver = await _context.Users.FindAsync(receiverId);

                if (sender != null && receiver != null)
                {
                    var subject = $"New Connection Request from {sender.DisplayName}";
                    var body = $@"
                        <h2>New Connection Request</h2>
                        <p><strong>{sender.DisplayName}</strong> wants to connect with you on AI Community Platform.</p>
                        <p><strong>Role:</strong> {sender.Role}</p>
                        {(!string.IsNullOrEmpty(sender.Bio) ? $"<p><strong>Bio:</strong> {sender.Bio}</p>" : "")}
                        <p>
                            <a href='https://your-domain.com/Profile/View/{sender.Id}' 
                               style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                               View Profile
                            </a>
                        </p>
                    ";

                    await _emailService.SendEmailAsync(receiver.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending connection request notification");
            }
        }

        public async Task SendMessageNotificationAsync(int senderId, int receiverId, string messageContent)
        {
            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                var receiver = await _context.Users.FindAsync(receiverId);

                if (sender != null && receiver != null)
                {
                    var subject = $"New Message from {sender.DisplayName}";
                    var preview = messageContent.Length > 100 ? messageContent.Substring(0, 100) + "..." : messageContent;

                    var body = $@"
                        <h2>New Message</h2>
                        <p>You have received a new message from <strong>{sender.DisplayName}</strong>:</p>
                        <div style='background-color: #F3F4F6; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                            <p style='margin: 0;'>{preview}</p>
                        </div>
                        <p>
                            <a href='https://your-domain.com/Messaging' 
                               style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                               Reply to Message
                            </a>
                        </p>
                    ";

                    await _emailService.SendEmailAsync(receiver.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message notification");
            }
        }

        public async Task SendJobStatusUpdateAsync(int applicationId, string newStatus)
        {
            try
            {
                var application = await _context.JobApplications
                    .Include(ja => ja.Job)
                    .Include(ja => ja.User)
                    .FirstOrDefaultAsync(ja => ja.Id == applicationId);

                if (application != null)
                {
                    var subject = $"Application Status Update - {application.Job.Title}";
                    var body = $@"
                        <h2>Application Status Update</h2>
                        <p>Your application status has been updated for the position: <strong>{application.Job.Title}</strong></p>
                        <p><strong>New Status:</strong> {newStatus}</p>
                        <p><strong>Applied:</strong> {application.AppliedDate:MMM dd, yyyy}</p>
                        <p>
                            <a href='https://your-domain.com/Jobs/MyApplications' 
                               style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                               View Applications
                            </a>
                        </p>
                    ";

                    await _emailService.SendEmailAsync(application.User.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending job status update notification");
            }
        }

        public async Task SendWelcomeEmailAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user != null)
                {
                    var subject = "Welcome to AI Community Platform!";
                    var body = $@"
                        <h2>Welcome to AI Community Platform, {user.FirstName}!</h2>
                        <p>Thank you for joining our community of AI professionals. Here's how to get started:</p>
                        
                        <h3>Complete Your Profile</h3>
                        <ul>
                            <li>Add your skills and experience</li>
                            <li>Upload a professional photo</li>
                            <li>Write a compelling bio</li>
                        </ul>
                        
                        <h3>Explore Opportunities</h3>
                        <ul>
                            <li>Browse AI job openings</li>
                            <li>Connect with other professionals</li>
                            <li>Join relevant groups</li>
                        </ul>
                        
                        <p>
                            <a href='https://your-domain.com/Profile/Edit' 
                               style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; margin-right: 10px;'>
                               Complete Profile
                            </a>
                            <a href='https://your-domain.com/Jobs' 
                               style='background-color: #059669; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                               Browse Jobs
                            </a>
                        </p>
                        
                        <p>Welcome aboard!</p>
                        <p>The AI Community Platform Team</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email");
            }
        }

        public async Task SendJobReminderAsync(int savedJobId)
        {
            try
            {
                var savedJob = await _context.SavedJobs
                    .Include(sj => sj.Job)
                    .Include(sj => sj.User)
                    .FirstOrDefaultAsync(sj => sj.Id == savedJobId);

                if (savedJob != null && savedJob.ReminderDate.HasValue &&
                    savedJob.ReminderDate.Value.Date <= DateTime.Now.Date && !savedJob.IsReminded)
                {
                    var subject = $"Reminder: {savedJob.Job.Title} Application";
                    var body = $@"
                        <h2>Job Application Reminder</h2>
                        <p>You saved this job and set a reminder to apply:</p>
                        <p><strong>Position:</strong> {savedJob.Job.Title}</p>
                        <p><strong>Company:</strong> {savedJob.Job.Organization.DisplayName}</p>
                        <p><strong>Location:</strong> {savedJob.Job.Location}</p>
                        {(savedJob.Job.Deadline.HasValue ? $"<p><strong>Application Deadline:</strong> {savedJob.Job.Deadline.Value:MMM dd, yyyy}</p>" : "")}
                        <p>
                            <a href='https://your-domain.com/Jobs/Details/{savedJob.Job.Id}' 
                               style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                               Apply Now
                            </a>
                        </p>
                    ";

                    await _emailService.SendEmailAsync(savedJob.User.Email, subject, body);

                    // Mark as reminded
                    savedJob.IsReminded = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending job reminder");
            }
        }
    }
}
