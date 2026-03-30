using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IEloRatingRepository : IGenericRepository<EloRating>
{
    Task<EloRating?> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
}
