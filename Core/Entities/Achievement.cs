using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Entities;

public class Achievement
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public AchievementType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Threshold { get; set; }
    public DateTime AwardedAtUtc { get; set; } = DateTime.UtcNow;
}
