using System.ComponentModel.DataAnnotations;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Entities;

public class EloRatingChange
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser? ApplicationUser { get; set; }

    public int? HabitId { get; set; }
    public Habit? Habit { get; set; }

    public DateOnly OccurredDate { get; set; }
    public HabitOutcomeType OutcomeType { get; set; }

    public int DifficultyBasisStreak { get; set; }
    public double DifficultyMultiplier { get; set; }

    public int Delta { get; set; }
    public int RatingAfterChange { get; set; }

    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
