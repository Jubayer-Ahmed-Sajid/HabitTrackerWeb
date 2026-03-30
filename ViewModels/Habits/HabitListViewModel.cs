namespace HabitTrackerWeb.ViewModels.Habits;

public class HabitListViewModel
{
    public IReadOnlyList<HabitListItemViewModel> Habits { get; set; } = [];
}
