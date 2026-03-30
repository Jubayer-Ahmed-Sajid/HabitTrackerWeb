namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IPushNotificationService
{
    bool IsEnabled { get; }

    string? GetPublicKey();

    Task<bool> GetUserPreferenceAsync(string userId, CancellationToken cancellationToken = default);

    Task SetUserPreferenceAsync(string userId, bool enabled, CancellationToken cancellationToken = default);

    Task SaveSubscriptionAsync(string userId, PushSubscriptionRegistration registration, CancellationToken cancellationToken = default);

    Task RemoveSubscriptionAsync(string userId, string endpoint, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetSubscribedUserIdsAsync(CancellationToken cancellationToken = default);

    Task<int> SendAsync(string userId, PushPayload payload, CancellationToken cancellationToken = default);
}

public sealed record PushSubscriptionRegistration(
    string Endpoint,
    string P256Dh,
    string Auth);

public sealed record PushPayload(
    string Title,
    string Message,
    string ActionUrl,
    string Tag);
