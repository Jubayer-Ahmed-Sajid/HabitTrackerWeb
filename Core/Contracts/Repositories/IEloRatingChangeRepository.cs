using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IEloRatingChangeRepository : IGenericRepository<EloRatingChange>
{
    Task<bool> ExistsForOutcomeAsync(
        string userId,
        int? habitId,
        DateOnly occurredDate,
        HabitOutcomeType outcomeType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EloRatingChange>> GetForUserRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
