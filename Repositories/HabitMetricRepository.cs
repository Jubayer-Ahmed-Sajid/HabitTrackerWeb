using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class HabitMetricRepository : GenericRepository<HabitMetric>, IHabitMetricRepository
{
    public HabitMetricRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<HabitMetric?> GetByHabitIdAsync(int habitId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(m => m.HabitId == habitId, cancellationToken);
    }
}
