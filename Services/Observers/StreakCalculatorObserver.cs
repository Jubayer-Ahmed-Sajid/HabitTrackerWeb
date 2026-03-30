using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.DomainEvents;
using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Services.Observers;

public class StreakCalculatorObserver : IStreakCalculatorObserver
{
    private readonly IUnitOfWork _unitOfWork;

    public StreakCalculatorObserver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task OnHabitCompletedAsync(HabitCompletedEvent habitCompletedEvent, CancellationToken cancellationToken = default)
    {
        var endDate = habitCompletedEvent.LoggedDate;
        var startDate = endDate.AddDays(-60);

        var logs = await _unitOfWork.HabitLogs.GetLogsForHabitRangeAsync(
            habitCompletedEvent.HabitId,
            habitCompletedEvent.UserId,
            startDate,
            endDate,
            cancellationToken);

        var completedDates = logs
            .Select(l => l.LoggedDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var currentStreak = CalculateCurrentStreak(completedDates, habitCompletedEvent.LoggedDate);
        var longestStreak = CalculateLongestStreak(completedDates);

        var last30Days = logs
            .Select(l => l.LoggedDate)
            .Distinct()
            .Count(d => d >= endDate.AddDays(-29) && d <= endDate);

        var metric = await _unitOfWork.HabitMetrics.GetByHabitIdAsync(habitCompletedEvent.HabitId, cancellationToken);
        var isNewMetric = false;
        if (metric is null)
        {
            metric = new HabitMetric
            {
                HabitId = habitCompletedEvent.HabitId
            };

            await _unitOfWork.HabitMetrics.AddAsync(metric, cancellationToken);
            isNewMetric = true;
        }

        metric.CurrentStreak = currentStreak;
        metric.LongestStreak = Math.Max(metric.LongestStreak, longestStreak);
        metric.LastCompletedDate = habitCompletedEvent.LoggedDate;
        metric.CompletionRate30Days = Math.Round((double)last30Days / 30 * 100, 1);
        metric.UpdatedAtUtc = DateTime.UtcNow;

        if (!isNewMetric)
        {
            _unitOfWork.HabitMetrics.Update(metric);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static int CalculateCurrentStreak(IReadOnlyCollection<DateOnly> completedDates, DateOnly fromDate)
    {
        if (completedDates.Count == 0)
        {
            return 0;
        }

        var dateSet = completedDates.ToHashSet();
        var streak = 0;
        var cursor = fromDate;

        while (dateSet.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static int CalculateLongestStreak(IReadOnlyList<DateOnly> dates)
    {
        if (dates.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;

        for (var i = 1; i < dates.Count; i++)
        {
            if (dates[i] == dates[i - 1].AddDays(1))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }
}
