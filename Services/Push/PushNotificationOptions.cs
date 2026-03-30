namespace HabitTrackerWeb.Services.Push;

public sealed class PushNotificationOptions
{
    public const string SectionName = "PushNotifications";

    public string Subject { get; set; } = "mailto:habittracker@local.dev";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
