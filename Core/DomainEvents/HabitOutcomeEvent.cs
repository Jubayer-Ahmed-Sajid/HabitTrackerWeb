using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.DomainEvents;

public sealed record HabitOutcomeEvent(
    int HabitId,
    string UserId,
    DateOnly Date,
    HabitOutcomeType OutcomeType,
    int StreakBeforeOutcome,
    int StreakAfterOutcome,
    DateTime OccurredAtUtc,
    string Reason);
