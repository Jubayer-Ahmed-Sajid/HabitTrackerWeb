using System.ComponentModel.DataAnnotations;

namespace HabitTrackerWeb.ViewModels.Push;

public sealed class PushUnsubscribeRequest
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
