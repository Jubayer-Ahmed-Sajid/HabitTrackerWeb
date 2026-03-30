namespace HabitTrackerWeb.Core.DomainEvents;

public sealed record HabitCompletedEvent(
    int HabitId,
    string UserId,
    DateOnly LoggedDate,
    DateTime OccurredAtUtc);
