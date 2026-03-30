using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class EloRatingRepository : GenericRepository<EloRating>, IEloRatingRepository
{
    public EloRatingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<EloRating?> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.ApplicationUserId == userId, cancellationToken);
    }
}
