using HabitTrackerWeb.Core.Contracts.Services;

namespace HabitTrackerWeb.Services.ExternalSync;

public abstract class ExternalSyncHandlerBase : IExternalSyncHandler
{
    private IExternalSyncHandler? _next;

    public abstract int Order { get; }

    public IExternalSyncHandler SetNext(IExternalSyncHandler next)
    {
        _next = next;
        return next;
    }

    public async Task HandleAsync(ExternalSyncContext context, CancellationToken cancellationToken = default)
    {
        await HandleCurrentAsync(context, cancellationToken);

        if (_next is not null)
        {
            await _next.HandleAsync(context, cancellationToken);
        }
    }

    protected abstract Task HandleCurrentAsync(ExternalSyncContext context, CancellationToken cancellationToken);
}
