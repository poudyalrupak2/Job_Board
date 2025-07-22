using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AICommunityPlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace AICommunityPlatform.Services
{
    public class JobReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobReminderService> _logger;

        public JobReminderService(IServiceProvider serviceProvider, ILogger<JobReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var reminderJobs = await context.SavedJobs
                        .Where(sj => sj.ReminderDate.HasValue &&
                                    sj.ReminderDate.Value.Date <= DateTime.Now.Date &&
                                    !sj.IsReminded)
                        .ToListAsync();

                    foreach (var savedJob in reminderJobs)
                    {
                        await notificationService.SendJobReminderAsync(savedJob.Id);
                    }

                    _logger.LogInformation($"Processed {reminderJobs.Count} job reminders");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job reminders");
                }

            }
        }
    }
}