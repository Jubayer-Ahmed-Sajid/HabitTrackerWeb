namespace HabitTrackerWeb.Core.Contracts.Services;

public interface INotificationService
{
    Task<IReadOnlyList<ProductivityNotification>> GetNotificationsAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
