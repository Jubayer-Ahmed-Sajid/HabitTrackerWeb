namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IExternalIntegrationService
{
    Task<IReadOnlyList<ExternalAccountLinkItem>> GetExternalLinksAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ExternalAccountLinkItem> UpsertExternalLinkAsync(
        string userId,
        ExternalAccountLinkUpsertCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteExternalLinkAsync(
        string userId,
        int externalLinkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HabitExternalSyncItem>> GetHabitSyncMappingsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateHabitSyncMappingAsync(
        string userId,
        int habitId,
        HabitExternalSyncUpdateCommand command,
        CancellationToken cancellationToken = default);
}
