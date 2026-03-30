namespace HabitTrackerWeb.ViewModels.Dashboard;

public class DashboardHabitItemViewModel
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool CompletedToday { get; set; }
    public int CurrentStreak { get; set; }
    public bool ShowFireIcon { get; set; }
}
