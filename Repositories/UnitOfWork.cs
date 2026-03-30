using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Data;

namespace HabitTrackerWeb.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(
        ApplicationDbContext context,
        ICategoryRepository categories,
        IHabitRepository habits,
        IHabitLogRepository habitLogs,
        IHabitMetricRepository habitMetrics,
        IAchievementRepository achievements,
        IExternalAccountLinkRepository? externalAccountLinks = null,
        IEloRatingRepository? eloRatings = null,
        IEloRatingChangeRepository? eloRatingChanges = null)
    {
        _context = context;
        Categories = categories;
        Habits = habits;
        HabitLogs = habitLogs;
        HabitMetrics = habitMetrics;
        Achievements = achievements;
        ExternalAccountLinks = externalAccountLinks ?? new ExternalAccountLinkRepository(context);
        EloRatings = eloRatings ?? new EloRatingRepository(context);
        EloRatingChanges = eloRatingChanges ?? new EloRatingChangeRepository(context);
    }

    public ICategoryRepository Categories { get; }
    public IHabitRepository Habits { get; }
    public IHabitLogRepository HabitLogs { get; }
    public IHabitMetricRepository HabitMetrics { get; }
    public IAchievementRepository Achievements { get; }
    public IExternalAccountLinkRepository ExternalAccountLinks { get; }
    public IEloRatingRepository EloRatings { get; }
    public IEloRatingChangeRepository EloRatingChanges { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
