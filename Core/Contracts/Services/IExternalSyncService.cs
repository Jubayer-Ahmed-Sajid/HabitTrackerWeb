namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IExternalSyncService
{
    Task<ExternalSyncRunResult> SyncDailyActivityAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}

public sealed record ExternalSyncRunResult(
    DateOnly Date,
    int UsersScanned,
    int HabitsAutoCompleted,
    int DataItemsProcessed);
