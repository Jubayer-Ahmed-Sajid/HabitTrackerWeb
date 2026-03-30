namespace HabitTrackerWeb.Core.Contracts.Repositories;

public interface IUnitOfWork
{
    ICategoryRepository Categories { get; }
    IHabitRepository Habits { get; }
    IHabitLogRepository HabitLogs { get; }
    IHabitMetricRepository HabitMetrics { get; }
    IAchievementRepository Achievements { get; }
    IExternalAccountLinkRepository ExternalAccountLinks { get; }
    IEloRatingRepository EloRatings { get; }
    IEloRatingChangeRepository EloRatingChanges { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
