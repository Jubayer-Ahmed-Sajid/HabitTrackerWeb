using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IAchievementRepository : IGenericRepository<Achievement>
{
    Task<bool> ExistsAsync(
        int habitId,
        string userId,
        AchievementType type,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Achievement>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
}
