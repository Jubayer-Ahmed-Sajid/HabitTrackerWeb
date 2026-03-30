namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IDashboardService
{
    Task<HabitDashboardSnapshot> GetDailyDashboardAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
