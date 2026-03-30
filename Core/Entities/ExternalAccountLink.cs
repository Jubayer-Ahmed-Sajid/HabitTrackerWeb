using System.ComponentModel.DataAnnotations;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Entities;

public class ExternalAccountLink
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser? ApplicationUser { get; set; }

    public ExternalActivitySource Source { get; set; }

    [Required]
    [MaxLength(120)]
    public string ExternalUserName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AccessToken { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}