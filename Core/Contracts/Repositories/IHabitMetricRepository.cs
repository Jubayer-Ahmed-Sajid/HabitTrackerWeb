using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IHabitMetricRepository : IGenericRepository<HabitMetric>
{
    Task<HabitMetric?> GetByHabitIdAsync(int habitId, CancellationToken cancellationToken = default);
}
