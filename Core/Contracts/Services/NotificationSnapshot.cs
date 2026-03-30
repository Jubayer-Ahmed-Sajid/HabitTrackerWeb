namespace HabitTrackerWeb.Core.Contracts.Services;

public enum ProductivityNotificationType
{
    Info = 1,
    Success = 2,
    Warning = 3
}

public sealed record ProductivityNotification(
    string Title,
    string Message,
    ProductivityNotificationType Type,
    string ActionLabel,
    string ActionUrl);
