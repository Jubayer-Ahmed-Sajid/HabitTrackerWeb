namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record RewardBadge(
    string Title,
    string HabitTitle,
    DateTime AwardedAtUtc,
    int Points);

public sealed record RewardMilestone(
    int TargetStreak,
    string Title,
    int BonusPoints);

public sealed record RewardSnapshot(
    int CurrentLevel,
    int TotalPoints,
    int NextLevelPoints,
    int PointsToNextLevel,
    double LevelProgressPercent,
    string MotivationalLine,
    IReadOnlyList<RewardBadge> RecentBadges,
    IReadOnlyList<RewardMilestone> UpcomingMilestones);
