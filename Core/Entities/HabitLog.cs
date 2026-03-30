using System.ComponentModel.DataAnnotations;

namespace HabitTrackerWeb.Core.Entities;

public class HabitLog
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public DateOnly LoggedDate { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
