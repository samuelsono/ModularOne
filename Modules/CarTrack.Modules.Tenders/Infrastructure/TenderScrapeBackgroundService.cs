using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Tenders;

/// <summary>
/// Processes queued scrape runs and enqueues due scheduled sources.
/// </summary>
public sealed class TenderScrapeBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TenderScrapeBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var queueTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        using var scheduleTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        var queueTask = RunQueueLoopAsync(queueTimer, stoppingToken);
        var scheduleTask = RunScheduleLoopAsync(scheduleTimer, stoppingToken);

        await Task.WhenAll(queueTask, scheduleTask);
    }

    private async Task RunQueueLoopAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var scrapeService = scope.ServiceProvider.GetRequiredService<ITenderScrapeService>();
                await scrapeService.ProcessQueuedRunsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Tender scrape worker tick failed.");
            }
        }
    }

    private async Task RunScheduleLoopAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        // Initial pass shortly after startup.
        await TickScheduleAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TickScheduleAsync(stoppingToken);
        }
    }

    private async Task TickScheduleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var scrapeService = scope.ServiceProvider.GetRequiredService<ITenderScrapeService>();
            await scrapeService.ExpireStaleMatchesAsync(stoppingToken);
            var enqueued = await scrapeService.EnqueueDueScheduledRunsAsync(stoppingToken);
            if (enqueued > 0)
            {
                logger.LogInformation("Enqueued {Count} scheduled tender scrape run(s).", enqueued);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Tender schedule tick failed.");
        }
    }
}
