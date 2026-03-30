using System.ComponentModel.DataAnnotations;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.ViewModels.Integrations;

public sealed class ExternalAccountLinkRequest
{
    public int? Id { get; set; }

    [Required]
    public ExternalActivitySource Source { get; set; }

    [Required]
    [StringLength(120)]
    public string ExternalUserName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AccessToken { get; set; }

    public bool IsActive { get; set; } = true;
}
