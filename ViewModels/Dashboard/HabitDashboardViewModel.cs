namespace HabitTrackerWeb.ViewModels.Dashboard;

public class HabitDashboardViewModel
{
    public DateOnly Date { get; set; }
    public int TotalHabits { get; set; }
    public int CompletedHabits { get; set; }
    public double CompletionPercent { get; set; }
    public int HotStreakHabits { get; set; }
    public RewardSummaryViewModel RewardSummary { get; set; } = new();
    public IReadOnlyList<DashboardNotificationViewModel> Notifications { get; set; } = [];
    public string SelectedMentalState { get; set; } = "focused";
    public string PsychologyIntroLine { get; set; } = string.Empty;
    public IReadOnlyList<string> MentalStateOptions { get; set; } = [];
    public IReadOnlyList<DashboardPsychologicalTipViewModel> PsychologicalTips { get; set; } = [];
    public IReadOnlyList<DashboardCategoryProgressViewModel> CategoryProgress { get; set; } = [];
    public IReadOnlyList<DashboardAchievementViewModel> RecentAchievements { get; set; } = [];
    public IReadOnlyList<DashboardHabitItemViewModel> Habits { get; set; } = [];
}

public class RewardSummaryViewModel
{
    public int CurrentLevel { get; set; }
    public int TotalPoints { get; set; }
    public int NextLevelPoints { get; set; }
    public int PointsToNextLevel { get; set; }
    public double LevelProgressPercent { get; set; }
    public string MotivationalLine { get; set; } = string.Empty;
    public IReadOnlyList<RewardBadgeViewModel> RecentBadges { get; set; } = [];
    public IReadOnlyList<RewardMilestoneViewModel> UpcomingMilestones { get; set; } = [];
}

public class RewardBadgeViewModel
{
    public string Title { get; set; } = string.Empty;
    public string HabitTitle { get; set; } = string.Empty;
    public DateTime AwardedAtUtc { get; set; }
    public int Points { get; set; }
}

public class RewardMilestoneViewModel
{
    public int TargetStreak { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BonusPoints { get; set; }
}

public class DashboardNotificationViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = "/Dashboard";
}

public class DashboardPsychologicalTipViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string WhyItWorks { get; set; } = string.Empty;
    public string ActionPrompt { get; set; } = string.Empty;
    public string IfThenPlan { get; set; } = string.Empty;
    public string RecoveryStep { get; set; } = string.Empty;
    public string ReflectionPrompt { get; set; } = string.Empty;
    public string TimeWindow { get; set; } = string.Empty;
    public IReadOnlyList<string> Steps { get; set; } = [];
}

public class DashboardCategoryProgressViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public int Completed { get; set; }
    public int Total { get; set; }
    public double CompletionPercent { get; set; }
}

public class DashboardAchievementViewModel
{
    public string Title { get; set; } = string.Empty;
    public string HabitTitle { get; set; } = string.Empty;
    public DateTime AwardedAtUtc { get; set; }
}
