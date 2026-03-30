using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.DomainEvents;
using HabitTrackerWeb.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Services;

public sealed class HabitOutcomeMonitorService : IHabitOutcomeMonitorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHabitOutcomePublisher _outcomePublisher;

    public HabitOutcomeMonitorService(IUnitOfWork unitOfWork, IHabitOutcomePublisher outcomePublisher)
    {
        _unitOfWork = unitOfWork;
        _outcomePublisher = outcomePublisher;
    }

    public async Task<int> ProcessMissedHabitOutcomesAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var activeHabits = await _unitOfWork.Habits.Query()
            .AsNoTracking()
            .Include(h => h.HabitMetric)
            .Where(h => h.IsActive)
            .ToListAsync(cancellationToken);

        var completedPairs = await _unitOfWork.HabitLogs.Query()
            .AsNoTracking()
            .Where(l => l.LoggedDate == targetDate)
            .Select(l => new { l.ApplicationUserId, l.HabitId })
            .ToListAsync(cancellationToken);

        var completedSet = completedPairs
            .Select(x => $"{x.ApplicationUserId}:{x.HabitId}")
            .ToHashSet(StringComparer.Ordinal);

        var published = 0;

        foreach (var habit in activeHabits)
        {
            if (!HabitScheduleEvaluator.IsScheduledForDate(habit, targetDate))
            {
                continue;
            }

            var completionKey = $"{habit.ApplicationUserId}:{habit.Id}";
            if (completedSet.Contains(completionKey))
            {
                continue;
            }

            var domainEvent = new HabitOutcomeEvent(
                HabitId: habit.Id,
                UserId: habit.ApplicationUserId,
                Date: targetDate,
                OutcomeType: HabitOutcomeType.Missed,
                StreakBeforeOutcome: habit.HabitMetric?.CurrentStreak ?? 0,
                StreakAfterOutcome: 0,
                OccurredAtUtc: DateTime.UtcNow,
                Reason: "Missed scheduled habit target");

            await _outcomePublisher.PublishAsync(domainEvent, cancellationToken);
            published++;
        }

        return published;
    }
}
