using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IHabitRepository : IGenericRepository<Habit>
{
    Task<IReadOnlyList<Habit>> GetHabitsForUserAsync(
        string userId,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<Habit?> GetByIdForUserAsync(
        int habitId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Habit>> GetExternalAutoSyncHabitsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUsersWithExternalAutoSyncHabitsAsync(
        CancellationToken cancellationToken = default);
}
