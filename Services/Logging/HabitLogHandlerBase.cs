namespace HabitTrackerWeb.Services.Logging;

public abstract class HabitLogHandlerBase : IHabitLogHandler
{
    private IHabitLogHandler? _next;

    public IHabitLogHandler SetNext(IHabitLogHandler next)
    {
        _next = next;
        return next;
    }

    public async Task HandleAsync(HabitLogContext context, CancellationToken cancellationToken = default)
    {
        if (!context.IsValid)
        {
            return;
        }

        await HandleInternalAsync(context, cancellationToken);

        if (context.IsValid && _next is not null)
        {
            await _next.HandleAsync(context, cancellationToken);
        }
    }

    protected abstract Task HandleInternalAsync(HabitLogContext context, CancellationToken cancellationToken);
}
