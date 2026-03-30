using System.Security.Cryptography;
using System.Text;
using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services.Push;

public sealed class PushNotificationBackgroundService : BackgroundService
{
    private static readonly TimeSpan DispatchInterval = TimeSpan.FromSeconds(90);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PushNotificationBackgroundService> _logger;
    private readonly Dictionary<string, string> _lastNotificationSignature = new(StringComparer.Ordinal);

    public PushNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PushNotificationBackgroundService> logger)
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
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        if (!pushService.IsEnabled)
        {
            return;
        }

        var userIds = await pushService.GetSubscribedUserIdsAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var userId in userIds)
        {
            var notifications = await notificationService.GetNotificationsAsync(userId, today, cancellationToken);
            if (notifications.Count == 0)
            {
                continue;
            }

            var signature = ComputeSignature(notifications);
            if (_lastNotificationSignature.TryGetValue(userId, out var previous)
                && string.Equals(previous, signature, StringComparison.Ordinal))
            {
                continue;
            }

            _lastNotificationSignature[userId] = signature;

            var topNotification = notifications[0];
            var payload = new PushPayload(
                Title: topNotification.Title,
                Message: topNotification.Message,
                ActionUrl: topNotification.ActionUrl,
                Tag: $"habit-{signature[..8]}");

            await pushService.SendAsync(userId, payload, cancellationToken);
        }
    }

    private static string ComputeSignature(IReadOnlyList<ProductivityNotification> notifications)
    {
        var source = string.Join("|", notifications.Select(n => $"{n.Title}:{n.Message}:{n.Type}:{n.ActionUrl}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash);
    }
}
