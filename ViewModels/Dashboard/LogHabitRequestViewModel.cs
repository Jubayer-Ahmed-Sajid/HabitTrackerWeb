using System.ComponentModel.DataAnnotations;

namespace HabitTrackerWeb.ViewModels.Dashboard;

public class LogHabitRequestViewModel
{
    [Required]
    public int HabitId { get; set; }
}
