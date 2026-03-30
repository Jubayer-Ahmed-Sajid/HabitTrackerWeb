using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IExternalActivityProvider
{
    ExternalActivitySource Source { get; }

    Task<IReadOnlyList<ExternalActivityItem>> PullDailyActivityAsync(
        ExternalAccountLink link,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
