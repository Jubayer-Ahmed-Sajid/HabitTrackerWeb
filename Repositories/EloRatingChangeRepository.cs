using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class EloRatingChangeRepository : GenericRepository<EloRatingChange>, IEloRatingChangeRepository
{
    public EloRatingChangeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsForOutcomeAsync(
        string userId,
        int? habitId,
        DateOnly occurredDate,
        HabitOutcomeType outcomeType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            x => x.ApplicationUserId == userId
                 && x.HabitId == habitId
                 && x.OccurredDate == occurredDate
                 && x.OutcomeType == outcomeType,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EloRatingChange>> GetForUserRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ApplicationUserId == userId
                        && x.OccurredDate >= startDate
                        && x.OccurredDate <= endDate)
            .OrderBy(x => x.OccurredDate)
            .ToListAsync(cancellationToken);
    }
}
