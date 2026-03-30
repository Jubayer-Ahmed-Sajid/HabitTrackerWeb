using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services;

public sealed class HabitOutcomeBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HabitOutcomeBackgroundService> _logger;

    private DateOnly? _lastProcessedDate;

    public HabitOutcomeBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<HabitOutcomeBackgroundService> logger)
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
            _logger.LogWarning(ex, "Habit outcome cycle failed.");
        }

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Habit outcome cycle failed.");
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var todayUtc = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var targetDate = todayUtc.AddDays(-1);

        if (_lastProcessedDate == targetDate)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var monitorService = scope.ServiceProvider.GetRequiredService<IHabitOutcomeMonitorService>();

        var count = await monitorService.ProcessMissedHabitOutcomesAsync(targetDate, cancellationToken);
        _lastProcessedDate = targetDate;

        _logger.LogInformation("Habit missed outcome scan completed for {Date}. Published events: {Count}", targetDate, count);
    }
}
