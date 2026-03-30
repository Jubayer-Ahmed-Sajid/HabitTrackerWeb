using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Services;

public sealed class ExternalIntegrationService : IExternalIntegrationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExternalIntegrationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ExternalAccountLinkItem>> GetExternalLinksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExternalAccountLinks.Query()
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId)
            .OrderBy(x => x.Source)
            .ThenBy(x => x.ExternalUserName)
            .Select(x => new ExternalAccountLinkItem(
                x.Id,
                x.Source,
                x.ExternalUserName,
                x.IsActive,
                x.LastSyncedAtUtc,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExternalAccountLinkItem> UpsertExternalLinkAsync(
        string userId,
        ExternalAccountLinkUpsertCommand command,
        CancellationToken cancellationToken = default)
    {
        var source = command.Source;
        if (source == ExternalActivitySource.None)
        {
            throw new InvalidOperationException("Source is required.");
        }

        var normalizedUserName = command.ExternalUserName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw new InvalidOperationException("External username is required.");
        }

        var duplicateExists = await _unitOfWork.ExternalAccountLinks.Query()
            .AnyAsync(x => x.ApplicationUserId == userId
                           && x.Source == source
                           && x.ExternalUserName == normalizedUserName
                           && (!command.Id.HasValue || x.Id != command.Id.Value),
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("A link already exists for this provider and username.");
        }

        ExternalAccountLink link;
        var isNew = !command.Id.HasValue;

        if (isNew)
        {
            link = new ExternalAccountLink
            {
                ApplicationUserId = userId,
                Source = source,
                ExternalUserName = normalizedUserName,
                AccessToken = string.IsNullOrWhiteSpace(command.AccessToken) ? null : command.AccessToken.Trim(),
                IsActive = command.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.ExternalAccountLinks.AddAsync(link, cancellationToken);
        }
        else
        {
            var existingLinkId = command.Id ?? throw new InvalidOperationException("Link id is required for updates.");

            link = await _unitOfWork.ExternalAccountLinks.Query()
                .FirstOrDefaultAsync(x => x.Id == existingLinkId && x.ApplicationUserId == userId, cancellationToken)
                ?? throw new KeyNotFoundException("External account link not found.");

            link.Source = source;
            link.ExternalUserName = normalizedUserName;
            link.AccessToken = string.IsNullOrWhiteSpace(command.AccessToken)
                ? link.AccessToken
                : command.AccessToken.Trim();
            link.IsActive = command.IsActive;

            _unitOfWork.ExternalAccountLinks.Update(link);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExternalAccountLinkItem(
            link.Id,
            link.Source,
            link.ExternalUserName,
            link.IsActive,
            link.LastSyncedAtUtc,
            link.CreatedAtUtc);
    }

    public async Task<bool> DeleteExternalLinkAsync(
        string userId,
        int externalLinkId,
        CancellationToken cancellationToken = default)
    {
        var link = await _unitOfWork.ExternalAccountLinks.Query()
            .FirstOrDefaultAsync(x => x.Id == externalLinkId && x.ApplicationUserId == userId, cancellationToken);

        if (link is null)
        {
            return false;
        }

        _unitOfWork.ExternalAccountLinks.Remove(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<HabitExternalSyncItem>> GetHabitSyncMappingsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Habits.Query()
            .AsNoTracking()
            .Where(h => h.ApplicationUserId == userId)
            .OrderBy(h => h.Title)
            .Select(h => new HabitExternalSyncItem(
                h.Id,
                h.Title,
                h.IsActive,
                h.AutoCompleteFromExternal,
                h.ExternalSource,
                h.ExternalMatchKey,
                h.Category != null ? h.Category.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateHabitSyncMappingAsync(
        string userId,
        int habitId,
        HabitExternalSyncUpdateCommand command,
        CancellationToken cancellationToken = default)
    {
        var habit = await _unitOfWork.Habits.GetByIdForUserAsync(habitId, userId, cancellationToken);
        if (habit is null)
        {
            return false;
        }

        if (command.AutoCompleteFromExternal && command.ExternalSource == ExternalActivitySource.None)
        {
            throw new InvalidOperationException("External source is required when auto sync is enabled.");
        }

        if (command.AutoCompleteFromExternal)
        {
            var hasActiveLink = await _unitOfWork.ExternalAccountLinks.Query()
                .AnyAsync(x => x.ApplicationUserId == userId
                               && x.IsActive
                               && x.Source == command.ExternalSource,
                    cancellationToken);

            if (!hasActiveLink)
            {
                throw new InvalidOperationException("Create an active external account link for this source first.");
            }
        }

        habit.AutoCompleteFromExternal = command.AutoCompleteFromExternal;
        habit.ExternalSource = command.AutoCompleteFromExternal
            ? command.ExternalSource
            : ExternalActivitySource.None;
        habit.ExternalMatchKey = command.AutoCompleteFromExternal && !string.IsNullOrWhiteSpace(command.ExternalMatchKey)
            ? command.ExternalMatchKey.Trim()
            : null;

        _unitOfWork.Habits.Update(habit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
