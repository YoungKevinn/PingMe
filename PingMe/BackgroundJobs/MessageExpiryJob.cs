using Microsoft.EntityFrameworkCore;
using PingMe.Data;

namespace PingMe.BackgroundJobs;

public class MessageExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageExpiryJob> _logger;

    public MessageExpiryJob(IServiceScopeFactory scopeFactory, ILogger<MessageExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageExpiryJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var expired = await db.Messages
                    .Where(m => !m.IsDeleted && m.ExpiresAt.HasValue && m.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expired.Count > 0)
                {
                    foreach (var msg in expired)
                    {
                        msg.IsDeleted  = true;
                        msg.Content    = "Tin nhắn đã thu hồi";
                        msg.UpdatedAt  = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Expired {Count} messages.", expired.Count);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in MessageExpiryJob.");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
