using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HabitDashboardSnapshot> GetDailyDashboardAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var habits = await _unitOfWork.Habits.GetHabitsForUserAsync(userId, activeOnly: true, cancellationToken);
        var achievements = await _unitOfWork.Achievements.GetForUserAsync(userId, cancellationToken);

        var scheduledHabits = habits
            .Where(h => HabitScheduleEvaluator.IsScheduledForDate(h, date))
            .ToList();

        var completedHabitIds = await _unitOfWork.HabitLogs
            .GetCompletedHabitIdsForDateAsync(userId, date, cancellationToken);

        var dashboardItems = scheduledHabits
            .Select(h =>
            {
                var streak = h.HabitMetric?.CurrentStreak ?? 0;
                var completed = completedHabitIds.Contains(h.Id);
                return new HabitDashboardItem(
                    HabitId: h.Id,
                    Title: h.Title,
                    Description: h.Description,
                    CategoryName: h.Category?.Name ?? "Uncategorized",
                    CompletedToday: completed,
                    CurrentStreak: streak,
                    ShowFire: streak >= 3,
                    IsActive: h.IsActive);
            })
            .OrderBy(i => i.CompletedToday)
            .ThenBy(i => i.Title)
            .ToList();

        var totalHabits = dashboardItems.Count;
        var completedHabits = dashboardItems.Count(i => i.CompletedToday);
        var completionPercent = totalHabits == 0 ? 0 : Math.Round((double)completedHabits / totalHabits * 100, 1);
        var hotStreakHabits = dashboardItems.Count(i => i.ShowFire);

        var categoryProgress = scheduledHabits
            .GroupBy(h => h.Category?.Name ?? "Uncategorized")
            .Select(group =>
            {
                var total = group.Count();
                var completed = group.Count(h => completedHabitIds.Contains(h.Id));
                var percent = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);
                return new DashboardCategoryProgress(group.Key, completed, total, percent);
            })
            .OrderByDescending(c => c.CompletionPercent)
            .ThenBy(c => c.CategoryName)
            .ToList();

        var recentAchievements = achievements
            .Take(5)
            .Select(a => new DashboardAchievementItem(
                Title: a.Title,
                HabitTitle: a.Habit?.Title ?? "Habit",
                AwardedAtUtc: a.AwardedAtUtc))
            .ToList();

        return new HabitDashboardSnapshot(
            Date: date,
            TotalHabits: totalHabits,
            CompletedHabits: completedHabits,
            CompletionPercent: completionPercent,
            HotStreakHabits: hotStreakHabits,
            CategoryProgress: categoryProgress,
            RecentAchievements: recentAchievements,
            Habits: dashboardItems);
    }
}
