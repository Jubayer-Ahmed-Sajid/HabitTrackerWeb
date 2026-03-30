using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class HabitRepository : GenericRepository<Habit>, IHabitRepository
{
    public HabitRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Habit>> GetHabitsForUserAsync(string userId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(h => h.Category)
            .Include(h => h.HabitMetric)
            .Where(h => h.ApplicationUserId == userId);

        if (activeOnly)
        {
            query = query.Where(h => h.IsActive);
        }

        return await query
            .OrderBy(h => h.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Habit?> GetByIdForUserAsync(int habitId, string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.Category)
            .Include(h => h.HabitMetric)
            .FirstOrDefaultAsync(
                h => h.Id == habitId && h.ApplicationUserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetExternalAutoSyncHabitsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(h => h.ApplicationUserId == userId
                        && h.IsActive
                        && h.AutoCompleteFromExternal
                        && h.ExternalSource != ExternalActivitySource.None)
            .OrderBy(h => h.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUsersWithExternalAutoSyncHabitsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(h => h.IsActive
                        && h.AutoCompleteFromExternal
                        && h.ExternalSource != ExternalActivitySource.None)
            .Select(h => h.ApplicationUserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
