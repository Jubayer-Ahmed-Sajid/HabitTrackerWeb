namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record HabitLogResult(
    bool Success,
    bool AlreadyLogged,
    int CurrentStreak,
    string Message);
