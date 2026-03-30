using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class HabitLogRepository : GenericRepository<HabitLog>, IHabitLogRepository
{
    public HabitLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsForDateAsync(int habitId, string userId, DateOnly loggedDate, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            l => l.HabitId == habitId && l.ApplicationUserId == userId && l.LoggedDate == loggedDate,
            cancellationToken);
    }

    public async Task<HashSet<int>> GetCompletedHabitIdsForDateAsync(string userId, DateOnly loggedDate, CancellationToken cancellationToken = default)
    {
        var habitIds = await DbSet
            .Where(l => l.ApplicationUserId == userId && l.LoggedDate == loggedDate)
            .Select(l => l.HabitId)
            .ToListAsync(cancellationToken);

        return habitIds.ToHashSet();
    }

    public async Task<IReadOnlyList<HabitLog>> GetLogsForHabitRangeAsync(
        int habitId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.HabitId == habitId
                        && l.ApplicationUserId == userId
                        && l.LoggedDate >= startDate
                        && l.LoggedDate <= endDate)
            .OrderBy(l => l.LoggedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HabitLog>> GetLogsForRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.ApplicationUserId == userId
                        && l.LoggedDate >= startDate
                        && l.LoggedDate <= endDate)
            .OrderBy(l => l.LoggedDate)
            .ToListAsync(cancellationToken);
    }
}
