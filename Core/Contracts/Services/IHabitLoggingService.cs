namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IHabitLoggingService
{
    Task<HabitLogResult> LogHabitCompletionAsync(
        int habitId,
        string userId,
        DateOnly loggedDate,
        CancellationToken cancellationToken = default);
}
