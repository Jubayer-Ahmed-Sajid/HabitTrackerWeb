using System.ComponentModel.DataAnnotations;
using HabitTrackerWeb.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HabitTrackerWeb.ViewModels.Habits;

public class HabitFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    [StringLength(80)]
    public string? SpecificDays { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];
}
