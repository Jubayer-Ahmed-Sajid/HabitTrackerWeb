using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.DomainEvents;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Services;

namespace HabitTrackerWeb.Services.Observers;

public class AchievementObserver : IAchievementObserver
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly (AchievementType Type, int Threshold, string Title)[] Rules =
    [
        (AchievementType.Streak3, 3, "Spark Ignited"),
        (AchievementType.Streak7, 7, "Momentum Week"),
        (AchievementType.Streak14, 14, "Fortnight Force"),
        (AchievementType.Streak30, 30, "Master Builder"),
        (AchievementType.Streak60, 60, "Unbreakable Discipline")
    ];

    public AchievementObserver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task OnHabitCompletedAsync(HabitCompletedEvent habitCompletedEvent, CancellationToken cancellationToken = default)
    {
        var metric = await _unitOfWork.HabitMetrics.GetByHabitIdAsync(habitCompletedEvent.HabitId, cancellationToken);
        if (metric is null)
        {
            return;
        }

        await AwardStreakAchievementsAsync(habitCompletedEvent, metric.CurrentStreak, cancellationToken);
        await AwardGoalAchievementsAsync(habitCompletedEvent, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task AwardStreakAchievementsAsync(
        HabitCompletedEvent habitCompletedEvent,
        int currentStreak,
        CancellationToken cancellationToken)
    {
        foreach (var rule in Rules)
        {
            if (currentStreak < rule.Threshold)
            {
                continue;
            }

            var awarded = await TryAwardAsync(
                habitCompletedEvent.HabitId,
                habitCompletedEvent.UserId,
                rule.Type,
                rule.Threshold,
                rule.Title,
                cancellationToken);

            if (!awarded)
            {
                continue;
            }
        }
    }

    private async Task AwardGoalAchievementsAsync(HabitCompletedEvent habitCompletedEvent, CancellationToken cancellationToken)
    {
        var activeHabits = await _unitOfWork.Habits.GetHabitsForUserAsync(
            habitCompletedEvent.UserId,
            activeOnly: true,
            cancellationToken);

        var scheduledToday = activeHabits
            .Where(h => HabitScheduleEvaluator.IsScheduledForDate(h, habitCompletedEvent.LoggedDate))
            .Select(h => h.Id)
            .ToHashSet();

        if (scheduledToday.Count > 0)
        {
            var completedToday = await _unitOfWork.HabitLogs.GetCompletedHabitIdsForDateAsync(
                habitCompletedEvent.UserId,
                habitCompletedEvent.LoggedDate,
                cancellationToken);

            var perfectDay = scheduledToday.All(completedToday.Contains);
            if (perfectDay)
            {
                await TryAwardAsync(
                    habitCompletedEvent.HabitId,
                    habitCompletedEvent.UserId,
                    AchievementType.PerfectDay,
                    threshold: scheduledToday.Count,
                    title: "Perfect Day Execution",
                    cancellationToken);
            }
        }

        var weekStart = habitCompletedEvent.LoggedDate.AddDays(-6);
        var weekLogs = await _unitOfWork.HabitLogs.GetLogsForRangeAsync(
            habitCompletedEvent.UserId,
            weekStart,
            habitCompletedEvent.LoggedDate,
            cancellationToken);

        var totalScheduled = 0;
        var totalCompleted = 0;

        for (var day = weekStart; day <= habitCompletedEvent.LoggedDate; day = day.AddDays(1))
        {
            var scheduled = activeHabits.Count(h => HabitScheduleEvaluator.IsScheduledForDate(h, day));
            var completed = weekLogs
                .Where(l => l.LoggedDate == day)
                .Select(l => l.HabitId)
                .Distinct()
                .Count();

            totalScheduled += scheduled;
            totalCompleted += completed;
        }

        if (totalScheduled > 0)
        {
            var weeklyPercent = (double)totalCompleted / totalScheduled * 100;
            if (weeklyPercent >= 80)
            {
                await TryAwardAsync(
                    habitCompletedEvent.HabitId,
                    habitCompletedEvent.UserId,
                    AchievementType.ConsistencyChampion,
                    threshold: 80,
                    title: "Consistency Champion",
                    cancellationToken);
            }
        }
    }

    private async Task<bool> TryAwardAsync(
        int habitId,
        string userId,
        AchievementType type,
        int threshold,
        string title,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await _unitOfWork.Achievements.ExistsAsync(
            habitId,
            userId,
            type,
            cancellationToken);

        if (alreadyExists)
        {
            return false;
        }

        await _unitOfWork.Achievements.AddAsync(new Achievement
        {
            HabitId = habitId,
            ApplicationUserId = userId,
            Type = type,
            Threshold = threshold,
            Title = title,
            AwardedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
