using System.ComponentModel.DataAnnotations;

namespace HabitTrackerWeb.Core.Entities;

public class EloRating
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser? ApplicationUser { get; set; }

    public int CurrentRating { get; set; } = 1000;
    public int PeakRating { get; set; } = 1000;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}