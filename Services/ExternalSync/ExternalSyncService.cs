using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.ExternalSync;

public sealed class ExternalSyncService : IExternalSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadOnlyDictionary<ExternalActivitySource, IExternalActivityProvider> _providerMap;
    private readonly IExternalSyncHandler? _rootHandler;
    private readonly ILogger<ExternalSyncService> _logger;

    public ExternalSyncService(
        IUnitOfWork unitOfWork,
        IEnumerable<IExternalActivityProvider> providers,
        IEnumerable<IExternalSyncHandler> handlers,
        ILogger<ExternalSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _providerMap = providers.ToDictionary(x => x.Source, x => x);
        _rootHandler = BuildChain(handlers);
        _logger = logger;
    }

    public async Task<ExternalSyncRunResult> SyncDailyActivityAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var usersWithHabits = await _unitOfWork.Habits.GetUsersWithExternalAutoSyncHabitsAsync(cancellationToken);
        var usersWithLinks = await _unitOfWork.ExternalAccountLinks.GetUsersWithActiveLinksAsync(cancellationToken);

        var users = usersWithHabits
            .Intersect(usersWithLinks, StringComparer.Ordinal)
            .ToList();

        if (users.Count == 0 || _rootHandler is null)
        {
            return new ExternalSyncRunResult(date, 0, 0, 0);
        }

        var totalHabitsAutoCompleted = 0;
        var totalItemsProcessed = 0;

        foreach (var userId in users)
        {
            var habits = await _unitOfWork.Habits.GetExternalAutoSyncHabitsForUserAsync(userId, cancellationToken);
            if (habits.Count == 0)
            {
                continue;
            }

            var links = await _unitOfWork.ExternalAccountLinks.GetActiveLinksForUserAsync(userId, cancellationToken);
            if (links.Count == 0)
            {
                continue;
            }

            var items = new List<ExternalActivityItem>();

            foreach (var link in links)
            {
                if (!_providerMap.TryGetValue(link.Source, out var provider))
                {
                    continue;
                }

                try
                {
                    var pulled = await provider.PullDailyActivityAsync(link, date, cancellationToken);
                    items.AddRange(pulled);
                    link.LastSyncedAtUtc = DateTime.UtcNow;
                    _unitOfWork.ExternalAccountLinks.Update(link);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "External sync provider failed for user {UserId} and source {Source}.",
                        userId,
                        link.Source);
                }
            }

            var context = new ExternalSyncContext(userId, date, habits, links, items);
            await _rootHandler.HandleAsync(context, cancellationToken);

            totalHabitsAutoCompleted += context.HabitsAutoCompleted;
            totalItemsProcessed += context.DataItemsProcessed;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExternalSyncRunResult(
            Date: date,
            UsersScanned: users.Count,
            HabitsAutoCompleted: totalHabitsAutoCompleted,
            DataItemsProcessed: totalItemsProcessed);
    }

    private static IExternalSyncHandler? BuildChain(IEnumerable<IExternalSyncHandler> handlers)
    {
        var ordered = handlers.OrderBy(x => x.Order).ToList();
        if (ordered.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < ordered.Count - 1; i++)
        {
            ordered[i].SetNext(ordered[i + 1]);
        }

        return ordered[0];
    }
}
