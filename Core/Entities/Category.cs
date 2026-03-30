using System.ComponentModel.DataAnnotations;

namespace HabitTrackerWeb.Core.Entities;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
}
