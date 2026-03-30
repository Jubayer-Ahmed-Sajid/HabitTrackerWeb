using System.ComponentModel.DataAnnotations;

namespace HabitTrackerWeb.ViewModels.Push;

public sealed class PushSubscriptionRequest
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string P256Dh { get; set; } = string.Empty;

    [Required]
    public string Auth { get; set; } = string.Empty;
}
