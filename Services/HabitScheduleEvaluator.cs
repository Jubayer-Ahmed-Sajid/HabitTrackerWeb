using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services;

public static class HabitScheduleEvaluator
{
    public static bool IsScheduledForDate(Habit habit, DateOnly date)
    {
        return habit.Frequency switch
        {
            HabitFrequency.Daily => true,
            HabitFrequency.Weekly => IsMatchBySpecificDays(habit, date) || date.DayOfWeek == habit.CreatedAtUtc.DayOfWeek,
            HabitFrequency.SpecificDays => IsMatchBySpecificDays(habit, date),
            _ => false
        };
    }

    private static bool IsMatchBySpecificDays(Habit habit, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(habit.SpecificDays))
        {
            return false;
        }

        var normalizedCurrent = date.DayOfWeek.ToString()[..3].ToLowerInvariant();
        var days = habit.SpecificDays
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d[..Math.Min(3, d.Length)].ToLowerInvariant());

        return days.Contains(normalizedCurrent);
    }
}
