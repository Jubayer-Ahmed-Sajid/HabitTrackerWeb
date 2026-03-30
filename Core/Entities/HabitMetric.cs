namespace HabitTrackerWeb.Core.Entities;

public class HabitMetric
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double CompletionRate30Days { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
