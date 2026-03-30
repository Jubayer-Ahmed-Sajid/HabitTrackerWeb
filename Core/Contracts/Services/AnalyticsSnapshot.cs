namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record WeeklyConsistencyPoint(DateOnly Date, int CompletedHabits, int TotalHabits, double CompletionPercent);

public sealed record CompletionTrendPoint(DateOnly Date, int CompletedHabits);

public sealed record CategoryPerformancePoint(
    string CategoryName,
    int ScheduledCount,
    int CompletedCount,
    double CompletionPercent);

public sealed record RecentAchievementItem(
    string Title,
    string HabitTitle,
    DateTime AwardedAtUtc);

public sealed record AnalyticsKpi(
    int ActiveHabits,
    int TotalCompletions,
    double AverageDailyCompletionPercent,
    string BestDayLabel,
    int BestDayCompletions,
    int HotStreakHabits,
    int CurrentEloRating,
    int PeakEloRating);

public sealed record HabitAnalyticsItem(
    int HabitId,
    string HabitTitle,
    int CurrentStreak,
    int LongestStreak,
    double CompletionRate30Days);

public sealed record ContributionHeatmapPoint(
    DateOnly Date,
    int CompletedHabits,
    int EloPointsGained,
    int IntensityLevel,
    bool AnyCompletion);

public sealed record AnalyticsSnapshot(
    int RangeDays,
    AnalyticsKpi Kpi,
    IReadOnlyList<WeeklyConsistencyPoint> WeeklyConsistency,
    IReadOnlyList<CompletionTrendPoint> CompletionTrend,
    IReadOnlyList<CategoryPerformancePoint> CategoryPerformance,
    IReadOnlyList<RecentAchievementItem> RecentAchievements,
    IReadOnlyList<string> Insights,
    IReadOnlyList<HabitAnalyticsItem> HabitStats);
