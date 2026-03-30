using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services;

public class RewardService : IRewardService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Dictionary<AchievementType, int> AchievementPointMap = new()
    {
        [AchievementType.Streak3] = 50,
        [AchievementType.Streak7] = 120,
        [AchievementType.Streak14] = 220,
        [AchievementType.Streak30] = 450,
        [AchievementType.Streak60] = 900,
        [AchievementType.PerfectDay] = 180,
        [AchievementType.ConsistencyChampion] = 300
    };

    private static readonly RewardMilestone[] Milestones =
    [
        new(3, "Spark Ignited", 50),
        new(7, "Momentum Week", 120),
        new(14, "Fortnight Force", 220),
        new(30, "Master Builder", 450),
        new(60, "Unbreakable Discipline", 900)
    ];

    public RewardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RewardSnapshot> GetRewardSnapshotAsync(
        string userId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var habits = await _unitOfWork.Habits.GetHabitsForUserAsync(userId, activeOnly: true, cancellationToken);
        var achievements = await _unitOfWork.Achievements.GetForUserAsync(userId, cancellationToken);

        var logs = await _unitOfWork.HabitLogs.GetLogsForRangeAsync(
            userId,
            asOfDate.AddDays(-29),
            asOfDate,
            cancellationToken);

        var basePoints = logs.Count * 5;
        var achievementPoints = achievements.Sum(a => AchievementPointMap.GetValueOrDefault(a.Type, 40));
        var totalPoints = basePoints + achievementPoints;

        const int levelSize = 300;
        var currentLevel = (totalPoints / levelSize) + 1;
        var nextLevelPoints = currentLevel * levelSize;
        var pointsToNext = Math.Max(nextLevelPoints - totalPoints, 0);
        var progress = Math.Round((double)(totalPoints % levelSize) / levelSize * 100, 1);

        var recentBadges = achievements
            .Take(5)
            .Select(a => new RewardBadge(
                Title: a.Title,
                HabitTitle: a.Habit?.Title ?? "Habit",
                AwardedAtUtc: a.AwardedAtUtc,
                Points: AchievementPointMap.GetValueOrDefault(a.Type, 40)))
            .ToList();

        var bestStreak = habits
            .Select(h => h.HabitMetric?.CurrentStreak ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        var upcomingMilestones = Milestones
            .Where(m => m.TargetStreak > bestStreak)
            .Take(3)
            .ToList();

        var motivationalLine = BuildMotivationLine(progress, pointsToNext, recentBadges.Count);

        return new RewardSnapshot(
            CurrentLevel: currentLevel,
            TotalPoints: totalPoints,
            NextLevelPoints: nextLevelPoints,
            PointsToNextLevel: pointsToNext,
            LevelProgressPercent: progress,
            MotivationalLine: motivationalLine,
            RecentBadges: recentBadges,
            UpcomingMilestones: upcomingMilestones);
    }

    private static string BuildMotivationLine(double progress, int pointsToNext, int recentBadges)
    {
        if (recentBadges > 0 && progress >= 75)
        {
            return $"You are close to leveling up. {pointsToNext} points to unlock your next tier.";
        }

        if (recentBadges > 0)
        {
            return "Momentum is building. Protect your current streaks to compound rewards.";
        }

        if (progress >= 45)
        {
            return "Solid trajectory. Consistent daily check-ins will trigger your first badges fast.";
        }

        return "Start with one non-negotiable completion today. Small wins unlock the reward loop.";
    }
}
