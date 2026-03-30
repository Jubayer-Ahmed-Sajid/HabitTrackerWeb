namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IExternalSyncHandler
{
    int Order { get; }

    IExternalSyncHandler SetNext(IExternalSyncHandler next);

    Task HandleAsync(ExternalSyncContext context, CancellationToken cancellationToken = default);
}
