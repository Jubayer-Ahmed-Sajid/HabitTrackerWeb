namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IAnalyticsService
{
    Task<AnalyticsSnapshot> GetAnalyticsAsync(
        string userId,
        int rangeDays,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContributionHeatmapPoint>> GetContributionHeatmapAsync(
        string userId,
        int days,
        CancellationToken cancellationToken = default);
}
