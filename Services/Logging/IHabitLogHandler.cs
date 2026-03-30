namespace HabitTrackerWeb.Services.Logging;

public interface IHabitLogHandler
{
    IHabitLogHandler SetNext(IHabitLogHandler next);
    Task HandleAsync(HabitLogContext context, CancellationToken cancellationToken = default);
}
