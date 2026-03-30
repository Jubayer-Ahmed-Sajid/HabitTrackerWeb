namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IHabitOutcomeMonitorService
{
    Task<int> ProcessMissedHabitOutcomesAsync(DateOnly targetDate, CancellationToken cancellationToken = default);
}
