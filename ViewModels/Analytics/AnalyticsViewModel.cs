namespace HabitTrackerWeb.ViewModels.Analytics;

public class AnalyticsViewModel
{
    public int SelectedRangeDays { get; set; } = 30;

    public int ActiveHabits { get; set; }
    public int TotalCompletions { get; set; }
    public double AverageDailyCompletionPercent { get; set; }
    public string BestDayLabel { get; set; } = "No data";
    public int BestDayCompletions { get; set; }
    public int HotStreakHabits { get; set; }
    public int CurrentEloRating { get; set; }
    public int PeakEloRating { get; set; }

    public string WeeklyLabelsJson { get; set; } = "[]";
    public string WeeklyCompletionJson { get; set; } = "[]";
    public string TrendLabelsJson { get; set; } = "[]";
    public string TrendDataJson { get; set; } = "[]";
    public string CategoryLabelsJson { get; set; } = "[]";
    public string CategoryCompletionJson { get; set; } = "[]";

    public IReadOnlyList<string> Insights { get; set; } = [];
    public IReadOnlyList<AchievementTimelineItemViewModel> RecentAchievements { get; set; } = [];
    public IReadOnlyList<CategoryPerformanceViewModel> CategoryPerformance { get; set; } = [];

    public IReadOnlyList<HabitAnalyticsRowViewModel> HabitStats { get; set; } = [];
}

public class CategoryPerformanceViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public int ScheduledCount { get; set; }
    public int CompletedCount { get; set; }
    public double CompletionPercent { get; set; }
}

public class AchievementTimelineItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string HabitTitle { get; set; } = string.Empty;
    public DateTime AwardedAtUtc { get; set; }
}

public class HabitAnalyticsRowViewModel
{
    public string HabitTitle { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double CompletionRate30Days { get; set; }
}
