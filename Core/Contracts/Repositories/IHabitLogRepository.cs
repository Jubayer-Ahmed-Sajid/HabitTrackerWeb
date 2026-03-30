using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IHabitLogRepository : IGenericRepository<HabitLog>
{
    Task<bool> ExistsForDateAsync(
        int habitId,
        string userId,
        DateOnly loggedDate,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetCompletedHabitIdsForDateAsync(
        string userId,
        DateOnly loggedDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HabitLog>> GetLogsForHabitRangeAsync(
        int habitId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HabitLog>> GetLogsForRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
