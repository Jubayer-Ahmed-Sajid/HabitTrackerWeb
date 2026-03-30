using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services.ExternalSync;

public sealed class ExternalSyncBackgroundService : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExternalSyncBackgroundService> _logger;

    public ExternalSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<ExternalSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunCycleAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External sync cycle failed.");
        }

        using var timer = new PeriodicTimer(SyncInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External sync cycle failed.");
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IExternalSyncService>();

        var utcToday = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var result = await syncService.SyncDailyActivityAsync(utcToday, cancellationToken);

        _logger.LogInformation(
            "External sync completed. Date={Date}, Users={Users}, AutoCompleted={Habits}, Items={Items}",
            result.Date,
            result.UsersScanned,
            result.HabitsAutoCompleted,
            result.DataItemsProcessed);
    }
}
