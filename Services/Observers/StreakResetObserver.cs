using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.DomainEvents;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.Observers;

public sealed class StreakResetObserver : IStreakResetObserver
{
    private readonly IUnitOfWork _unitOfWork;

    public StreakResetObserver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task HandleOutcomeAsync(HabitOutcomeEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.OutcomeType != HabitOutcomeType.Missed)
        {
            return;
        }

        var metric = await _unitOfWork.HabitMetrics.GetByHabitIdAsync(domainEvent.HabitId, cancellationToken);
        if (metric is null || metric.CurrentStreak == 0)
        {
            return;
        }

        metric.CurrentStreak = 0;
        metric.UpdatedAtUtc = DateTime.UtcNow;

        _unitOfWork.HabitMetrics.Update(metric);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
