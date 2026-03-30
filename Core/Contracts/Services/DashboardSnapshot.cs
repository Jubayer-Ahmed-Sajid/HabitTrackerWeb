namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record HabitDashboardItem(
    int HabitId,
    string Title,
    string? Description,
    string CategoryName,
    bool CompletedToday,
    int CurrentStreak,
    bool ShowFire,
    bool IsActive);

public sealed record DashboardCategoryProgress(
    string CategoryName,
    int Completed,
    int Total,
    double CompletionPercent);

public sealed record DashboardAchievementItem(
    string Title,
    string HabitTitle,
    DateTime AwardedAtUtc);

public sealed record HabitDashboardSnapshot(
    DateOnly Date,
    int TotalHabits,
    int CompletedHabits,
    double CompletionPercent,
    int HotStreakHabits,
    IReadOnlyList<DashboardCategoryProgress> CategoryProgress,
    IReadOnlyList<DashboardAchievementItem> RecentAchievements,
    IReadOnlyList<HabitDashboardItem> Habits);
