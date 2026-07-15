namespace CarTrack.Modules.Leave;

public class LeaveAccrualBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAccrualAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunAccrualAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var scope = scopeFactory.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaveAccrualBackgroundService>>();
                logger.LogWarning(ex, "Leave accrual job failed.");
            }
        }
    }

    private async Task RunAccrualAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var accrualService = scope.ServiceProvider.GetRequiredService<ILeaveAccrualService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaveAccrualBackgroundService>>();

        var applied = await accrualService.RunMonthlyAccrualAsync(cancellationToken);
        if (applied > 0)
        {
            logger.LogInformation("Applied {Count} leave accrual entries.", applied);
        }
    }
}
