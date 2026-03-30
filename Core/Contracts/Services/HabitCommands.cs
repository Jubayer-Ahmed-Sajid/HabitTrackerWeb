using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record HabitUpsertCommand(
    string Title,
    string? Description,
    HabitFrequency Frequency,
    string? SpecificDays,
    int CategoryId,
    bool IsActive = true);
