using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IExternalAccountLinkRepository : IGenericRepository<ExternalAccountLink>
{
    Task<IReadOnlyList<ExternalAccountLink>> GetActiveLinksForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalAccountLink>> GetActiveLinksForUserAndSourceAsync(
        string userId,
        ExternalActivitySource source,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUsersWithActiveLinksAsync(CancellationToken cancellationToken = default);
}
