using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IReadOnlyList<Category>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}
