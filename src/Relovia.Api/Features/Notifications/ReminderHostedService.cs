using Microsoft.Extensions.Options;

namespace Relovia.Api.Features.Notifications;

/// <summary>
/// Runs the reminder scan and the email outbox in this process. One pass creates reminders,
/// then sends the emails that are due.
/// </summary>
public sealed class ReminderHostedService(
    IServiceScopeFactory scopes,
    IOptions<ReminderSettings> options,
    ILogger<ReminderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Reminders are turned off.");
            return;
        }

        var interval = options.Value.Interval < TimeSpan.FromMinutes(1)
            ? TimeSpan.FromMinutes(1)
            : options.Value.Interval;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();
                await reminders.ScanAsync(stoppingToken);
                await reminders.DeliverAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The reminder pass failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
