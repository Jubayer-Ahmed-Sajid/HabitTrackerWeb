using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Core.Contracts.Services;

public sealed record ExternalAccountLinkItem(
    int Id,
    ExternalActivitySource Source,
    string ExternalUserName,
    bool IsActive,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedAtUtc);

public sealed record ExternalAccountLinkUpsertCommand(
    int? Id,
    ExternalActivitySource Source,
    string ExternalUserName,
    string? AccessToken,
    bool IsActive);

public sealed record HabitExternalSyncItem(
    int HabitId,
    string HabitTitle,
    bool IsActive,
    bool AutoCompleteFromExternal,
    ExternalActivitySource ExternalSource,
    string? ExternalMatchKey,
    string? CategoryName);

public sealed record HabitExternalSyncUpdateCommand(
    bool AutoCompleteFromExternal,
    ExternalActivitySource ExternalSource,
    string? ExternalMatchKey);
