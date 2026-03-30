using System.ComponentModel.DataAnnotations;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Entities;

public class Habit
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    [MaxLength(80)]
    public string? SpecificDays { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser? ApplicationUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ExternalActivitySource ExternalSource { get; set; } = ExternalActivitySource.None;

    [MaxLength(120)]
    public string? ExternalMatchKey { get; set; }

    public bool AutoCompleteFromExternal { get; set; }

    public ICollection<HabitLog> HabitLogs { get; set; } = new List<HabitLog>();
    public HabitMetric? HabitMetric { get; set; }
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public ICollection<EloRatingChange> EloRatingChanges { get; set; } = new List<EloRatingChange>();
}
