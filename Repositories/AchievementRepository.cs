using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class AchievementRepository : GenericRepository<Achievement>, IAchievementRepository
{
    public AchievementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(int habitId, string userId, AchievementType type, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            a => a.HabitId == habitId && a.ApplicationUserId == userId && a.Type == type,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Achievement>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Habit)
            .Where(a => a.ApplicationUserId == userId)
            .OrderByDescending(a => a.AwardedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
