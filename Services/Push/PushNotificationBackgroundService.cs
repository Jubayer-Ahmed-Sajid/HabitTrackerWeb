using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using Microsoft.Extensions.Options;

namespace HabitTrackerWeb.Services.Push;

public sealed class PushNotificationBackgroundService : BackgroundService
{
    private static readonly TimeSpan DispatchInterval = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _serviceProvider;
    private readonly PushNotificationOptions _options;
    private readonly ILogger<PushNotificationBackgroundService> _logger;
    private readonly Dictionary<string, DateOnly> _lastDailyReminderDate = new(StringComparer.Ordinal);

    public PushNotificationBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<PushNotificationOptions> options,
        ILogger<PushNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notification dispatch cycle failed.");
            }

            await Task.Delay(DispatchInterval, stoppingToken);
        }
    }

    private async Task DispatchNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (!pushService.IsEnabled)
        {
            return;
        }

        var userIds = await pushService.GetSubscribedUserIdsAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var hour = Math.Clamp(_options.DailyReminderHourUtc, 0, 23);
        var minute = Math.Clamp(_options.DailyReminderMinuteUtc, 0, 59);

        if (nowUtc.Hour < hour || (nowUtc.Hour == hour && nowUtc.Minute < minute))
        {
            return;
        }

        var today = DateOnly.FromDateTime(nowUtc);
        foreach (var userId in userIds)
        {
            if (_lastDailyReminderDate.TryGetValue(userId, out var lastSentDate) && lastSentDate == today)
            {
                continue;
            }

            var habits = await unitOfWork.Habits.GetHabitsForUserAsync(userId, activeOnly: true, cancellationToken);
            var scheduledTodayIds = habits
                .Where(h => HabitScheduleEvaluator.IsScheduledForDate(h, today))
                .Select(h => h.Id)
                .ToHashSet();

            if (scheduledTodayIds.Count == 0)
            {
                continue;
            }

            var completedTodayIds = await unitOfWork.HabitLogs.GetCompletedHabitIdsForDateAsync(userId, today, cancellationToken);
            var incompleteCount = scheduledTodayIds.Count(id => !completedTodayIds.Contains(id));

            if (incompleteCount <= 0)
            {
                continue;
            }

            var payload = new PushPayload(
                Title: "Daily habit reminder",
                Message: $"You still have {incompleteCount} incomplete habit(s) today. Complete one now to protect momentum.",
                ActionUrl: "/Dashboard",
                Tag: $"daily-reminder-{today:yyyyMMdd}");

            var delivered = await pushService.SendAsync(userId, payload, cancellationToken);
            if (delivered > 0)
            {
                _lastDailyReminderDate[userId] = today;
            }
        }
    }
}
