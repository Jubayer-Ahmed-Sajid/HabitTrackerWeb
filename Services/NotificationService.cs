using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductivityNotification>> GetNotificationsAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var notifications = new List<ProductivityNotification>();

        var habits = await _unitOfWork.Habits.GetHabitsForUserAsync(userId, activeOnly: true, cancellationToken);
        var scheduledToday = habits.Count(h => HabitScheduleEvaluator.IsScheduledForDate(h, date));

        var completedTodaySet = await _unitOfWork.HabitLogs.GetCompletedHabitIdsForDateAsync(userId, date, cancellationToken);
        var completedToday = completedTodaySet.Count;

        if (scheduledToday == 0)
        {
            notifications.Add(new ProductivityNotification(
                Title: "No habits scheduled",
                Message: "Today has no scheduled habits. Consider adding one micro-habit to maintain rhythm.",
                Type: ProductivityNotificationType.Info,
                ActionLabel: "Create Habit",
                ActionUrl: "/Habits/Create"));
        }
        else if (completedToday == 0)
        {
            notifications.Add(new ProductivityNotification(
                Title: "Start your first win",
                Message: "No completions logged yet today. Do one quick habit now to build momentum.",
                Type: ProductivityNotificationType.Warning,
                ActionLabel: "Open Focus Board",
                ActionUrl: "/Dashboard"));
        }
        else if (completedToday < scheduledToday)
        {
            notifications.Add(new ProductivityNotification(
                Title: "Momentum in progress",
                Message: $"You completed {completedToday}/{scheduledToday} habits. Finish one more to stay on track.",
                Type: ProductivityNotificationType.Info,
                ActionLabel: "Continue Logging",
                ActionUrl: "/Dashboard"));
        }
        else
        {
            notifications.Add(new ProductivityNotification(
                Title: "Perfect execution",
                Message: "All scheduled habits completed today. Great job protecting your system.",
                Type: ProductivityNotificationType.Success,
                ActionLabel: "View Analytics",
                ActionUrl: "/Analytics"));
        }

        var nearFireHabits = habits
            .Where(h => completedTodaySet.Contains(h.Id) && (h.HabitMetric?.CurrentStreak ?? 0) == 2)
            .Select(h => h.Title)
            .Take(2)
            .ToList();

        if (nearFireHabits.Count > 0)
        {
            notifications.Add(new ProductivityNotification(
                Title: "Fire streak is one day away",
                Message: $"{string.Join(", ", nearFireHabits)} can reach hot streak status on your next completion.",
                Type: ProductivityNotificationType.Success,
                ActionLabel: "Protect Streaks",
                ActionUrl: "/Dashboard"));
        }

        var achievement = (await _unitOfWork.Achievements.GetForUserAsync(userId, cancellationToken))
            .FirstOrDefault(a => a.AwardedAtUtc >= DateTime.UtcNow.AddDays(-2));

        if (achievement is not null)
        {
            notifications.Add(new ProductivityNotification(
                Title: "New reward unlocked",
                Message: $"{achievement.Title} badge earned from {achievement.Habit?.Title ?? "your habit"}.",
                Type: ProductivityNotificationType.Success,
                ActionLabel: "Open Rewards",
                ActionUrl: "/Dashboard#rewards"));
        }

        return notifications.Take(5).ToList();
    }
}
