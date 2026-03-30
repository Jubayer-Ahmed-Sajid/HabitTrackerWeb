namespace HabitTrackerWeb.Services.Push;

public sealed class PushNotificationOptions
{
    public const string SectionName = "PushNotifications";

    public string Subject { get; set; } = "mailto:habittracker@local.dev";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public int DailyReminderHourUtc { get; set; } = 18;
    public int DailyReminderMinuteUtc { get; set; } = 0;
}
