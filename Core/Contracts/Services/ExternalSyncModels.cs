using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record ExternalActivityItem(
    ExternalActivitySource Source,
    DateOnly ActivityDate,
    string ExternalUserName,
    string? MatchKey,
    int Quantity,
    string Description);

public sealed record ExternalSyncContext(
    string UserId,
    DateOnly Date,
    IReadOnlyList<Habit> AutoSyncHabits,
    IReadOnlyList<ExternalAccountLink> ActiveLinks,
    IReadOnlyList<ExternalActivityItem> ActivityItems)
{
    public int HabitsAutoCompleted { get; set; }
    public int DataItemsProcessed { get; set; }
}
