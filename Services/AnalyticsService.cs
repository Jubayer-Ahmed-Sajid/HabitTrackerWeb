using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _dbContext;

    public AnalyticsService(IUnitOfWork unitOfWork, ApplicationDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task<AnalyticsSnapshot> GetAnalyticsAsync(
        string userId,
        int rangeDays,
        CancellationToken cancellationToken = default)
    {
        var normalizedRangeDays = NormalizeRange(rangeDays);
        var habits = await _unitOfWork.Habits.GetHabitsForUserAsync(userId, activeOnly: true, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var rangeStart = today.AddDays(-(normalizedRangeDays - 1));
        var weekStart = today.AddDays(-6);

        var recentLogs = await _unitOfWork.HabitLogs
            .GetLogsForRangeAsync(userId, rangeStart, today, cancellationToken);

        var achievements = await _unitOfWork.Achievements.GetForUserAsync(userId, cancellationToken);

        var dayStats = BuildDayStats(habits, recentLogs, rangeStart, today);

        var weeklyConsistency = new List<WeeklyConsistencyPoint>();
        for (var day = weekStart; day <= today; day = day.AddDays(1))
        {
            if (!dayStats.TryGetValue(day, out var stat))
            {
                stat = (0, 0, 0);
            }

            weeklyConsistency.Add(new WeeklyConsistencyPoint(day, stat.Completed, stat.Scheduled, stat.Percent));
        }

        var completionTrend = new List<CompletionTrendPoint>();
        for (var day = rangeStart; day <= today; day = day.AddDays(1))
        {
            var completedCount = dayStats.TryGetValue(day, out var stat)
                ? stat.Completed
                : 0;

            completionTrend.Add(new CompletionTrendPoint(day, completedCount));
        }

        var categoryPerformance = habits
            .GroupBy(h => h.Category?.Name ?? "Uncategorized")
            .Select(group =>
            {
                var scheduled = 0;
                for (var day = rangeStart; day <= today; day = day.AddDays(1))
                {
                    scheduled += group.Count(h => HabitScheduleEvaluator.IsScheduledForDate(h, day));
                }

                var habitIds = group.Select(h => h.Id).ToHashSet();
                var completed = recentLogs.Count(l => habitIds.Contains(l.HabitId));

                var percent = scheduled == 0
                    ? 0
                    : Math.Round((double)completed / scheduled * 100, 1);

                return new CategoryPerformancePoint(group.Key, scheduled, completed, percent);
            })
            .OrderByDescending(c => c.CompletionPercent)
            .ThenBy(c => c.CategoryName)
            .ToList();

        var activeHabits = habits.Count;
        var totalCompletions = recentLogs.Count;
        var eloRating = await _unitOfWork.EloRatings.GetForUserAsync(userId, cancellationToken);

        var consideredDays = dayStats.Values.Where(v => v.Scheduled > 0).ToList();
        var averageDailyPercent = consideredDays.Count == 0
            ? 0
            : Math.Round(consideredDays.Average(v => v.Percent), 1);

        var bestDay = completionTrend
            .OrderByDescending(d => d.CompletedHabits)
            .ThenByDescending(d => d.Date)
            .FirstOrDefault();

        var bestDayLabel = bestDay is null
            ? "No data"
            : bestDay.Date.ToString("ddd, MMM dd");

        var hotStreakHabits = habits.Count(h => (h.HabitMetric?.CurrentStreak ?? 0) >= 3);

        var kpi = new AnalyticsKpi(
            ActiveHabits: activeHabits,
            TotalCompletions: totalCompletions,
            AverageDailyCompletionPercent: averageDailyPercent,
            BestDayLabel: bestDayLabel,
            BestDayCompletions: bestDay?.CompletedHabits ?? 0,
            HotStreakHabits: hotStreakHabits,
            CurrentEloRating: eloRating?.CurrentRating ?? 1000,
            PeakEloRating: eloRating?.PeakRating ?? 1000);

        var recentAchievementItems = achievements
            .Take(6)
            .Select(a => new RecentAchievementItem(
                Title: a.Title,
                HabitTitle: a.Habit?.Title ?? "Habit",
                AwardedAtUtc: a.AwardedAtUtc))
            .ToList();

        var insights = BuildInsights(kpi, categoryPerformance, recentAchievementItems);

        var habitStats = habits
            .Select(h =>
            {
                var metric = h.HabitMetric;
                return new HabitAnalyticsItem(
                    HabitId: h.Id,
                    HabitTitle: h.Title,
                    CurrentStreak: metric?.CurrentStreak ?? 0,
                    LongestStreak: metric?.LongestStreak ?? 0,
                    CompletionRate30Days: Math.Round(metric?.CompletionRate30Days ?? 0, 1));
            })
            .OrderByDescending(h => h.CurrentStreak)
            .ThenBy(h => h.HabitTitle)
            .ToList();

        return new AnalyticsSnapshot(
            RangeDays: normalizedRangeDays,
            Kpi: kpi,
            WeeklyConsistency: weeklyConsistency,
            CompletionTrend: completionTrend,
            CategoryPerformance: categoryPerformance,
            RecentAchievements: recentAchievementItems,
            Insights: insights,
            HabitStats: habitStats);
    }

    private static int NormalizeRange(int rangeDays)
    {
        var allowed = new[] { 14, 30, 90 };
        return allowed.Contains(rangeDays) ? rangeDays : 30;
    }

    private static Dictionary<DateOnly, (int Scheduled, int Completed, double Percent)> BuildDayStats(
        IReadOnlyList<Core.Entities.Habit> habits,
        IReadOnlyList<Core.Entities.HabitLog> logs,
        DateOnly startDate,
        DateOnly endDate)
    {
        var stats = new Dictionary<DateOnly, (int Scheduled, int Completed, double Percent)>();

        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var scheduled = habits.Count(h => HabitScheduleEvaluator.IsScheduledForDate(h, day));
            var completed = logs
                .Where(l => l.LoggedDate == day)
                .Select(l => l.HabitId)
                .Distinct()
                .Count();

            var percent = scheduled == 0
                ? 0
                : Math.Round((double)completed / scheduled * 100, 1);

            stats[day] = (scheduled, completed, percent);
        }

        return stats;
    }

    private static IReadOnlyList<string> BuildInsights(
        AnalyticsKpi kpi,
        IReadOnlyList<CategoryPerformancePoint> categoryPerformance,
        IReadOnlyList<RecentAchievementItem> achievements)
    {
        var insights = new List<string>();

        if (kpi.AverageDailyCompletionPercent >= 80)
        {
            insights.Add("Excellent consistency. You are sustaining an elite execution rhythm.");
        }
        else if (kpi.AverageDailyCompletionPercent >= 60)
        {
            insights.Add("Strong progress. A small increase in evening check-ins can push you past 80%.");
        }
        else
        {
            insights.Add("Consistency is unstable. Reduce daily load or tighten your completion window.");
        }

        var topCategory = categoryPerformance.FirstOrDefault();
        if (topCategory is not null)
        {
            insights.Add($"Top category: {topCategory.CategoryName} at {topCategory.CompletionPercent}% completion.");
        }

        if (kpi.HotStreakHabits > 0)
        {
            insights.Add($"You have {kpi.HotStreakHabits} hot streak habit(s). Protect these first each day.");
        }

        if (achievements.Count > 0)
        {
            insights.Add("Recent achievements indicate momentum. Use them as anchor behaviors for new habits.");
        }

        return insights;
    }

    public async Task<IReadOnlyList<ContributionHeatmapPoint>> GetContributionHeatmapAsync(
        string userId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var normalizedDays = Math.Clamp(days, 30, 365);
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var startDate = endDate.AddDays(-(normalizedDays - 1));

        var completionRows = await _dbContext.HabitLogs
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && x.LoggedDate >= startDate && x.LoggedDate <= endDate)
            .GroupBy(x => x.LoggedDate)
            .Select(g => new
            {
                Date = g.Key,
                CompletedHabits = g.Select(x => x.HabitId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        var eloRows = await _dbContext.EloRatingChanges
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && x.OccurredDate >= startDate && x.OccurredDate <= endDate)
            .GroupBy(x => x.OccurredDate)
            .Select(g => new
            {
                Date = g.Key,
                EloGained = g.Where(x => x.Delta > 0).Sum(x => x.Delta)
            })
            .ToListAsync(cancellationToken);

        var completedByDate = completionRows.ToDictionary(x => x.Date, x => x.CompletedHabits);
        var eloByDate = eloRows.ToDictionary(x => x.Date, x => x.EloGained);

        var result = new List<ContributionHeatmapPoint>(normalizedDays);
        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var completedHabits = completedByDate.GetValueOrDefault(day, 0);
            var eloGained = eloByDate.GetValueOrDefault(day, 0);

            result.Add(new ContributionHeatmapPoint(
                Date: day,
                CompletedHabits: completedHabits,
                EloPointsGained: eloGained,
                IntensityLevel: ComputeIntensityLevel(completedHabits, eloGained),
                AnyCompletion: completedHabits > 0));
        }

        return result;
    }

    private static int ComputeIntensityLevel(int completedHabits, int eloGained)
    {
        if (completedHabits <= 0 && eloGained <= 0)
        {
            return 0;
        }

        if (completedHabits <= 1 && eloGained <= 4)
        {
            return 1;
        }

        if (completedHabits <= 3 && eloGained <= 12)
        {
            return 2;
        }

        if (completedHabits <= 5 && eloGained <= 24)
        {
            return 3;
        }

        return 4;
    }
}
