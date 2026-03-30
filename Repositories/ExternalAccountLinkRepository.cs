using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Repositories;

public class ExternalAccountLinkRepository : GenericRepository<ExternalAccountLink>, IExternalAccountLinkRepository
{
    public ExternalAccountLinkRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ExternalAccountLink>> GetActiveLinksForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ApplicationUserId == userId && x.IsActive)
            .OrderBy(x => x.Source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalAccountLink>> GetActiveLinksForUserAndSourceAsync(
        string userId,
        ExternalActivitySource source,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.ApplicationUserId == userId && x.IsActive && x.Source == source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUsersWithActiveLinksAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsActive)
            .Select(x => x.ApplicationUserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
