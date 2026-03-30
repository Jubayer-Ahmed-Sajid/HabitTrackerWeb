using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Services.Logging;

public class HabitLogContext
{
    public int HabitId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public DateOnly LoggedDate { get; init; }

    public Habit? Habit { get; set; }
    public HabitLog? PersistedLog { get; set; }

    public bool IsValid { get; private set; } = true;
    public bool AlreadyLogged { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public void Invalidate(string message, bool alreadyLogged = false)
    {
        IsValid = false;
        AlreadyLogged = alreadyLogged;
        Message = message;
    }
}
