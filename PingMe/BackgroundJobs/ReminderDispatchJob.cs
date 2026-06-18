using PingMe.DTOs;
using PingMe.Services;

namespace PingMe.BackgroundJobs;

public class ReminderDispatchJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderDispatchJob> _logger;

    public ReminderDispatchJob(IServiceScopeFactory scopeFactory, ILogger<ReminderDispatchJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderDispatchJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();
                var messages = scope.ServiceProvider.GetRequiredService<IMessageService>();

                var dueReminders = await reminders.GetDueRemindersAsync(DateTime.UtcNow, 20);
                foreach (var reminder in dueReminders)
                {
                    await messages.SendReminderDueMessageAsync(reminder);
                    await reminders.MarkSentAsync(reminder.Id);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in ReminderDispatchJob.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
