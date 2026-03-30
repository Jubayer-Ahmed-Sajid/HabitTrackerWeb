using HabitTrackerWeb.Core.DomainEvents;

namespace HabitTrackerWeb.Core.Contracts.Observers;

public interface IHabitOutcomePublisher
{
    Task PublishAsync(HabitOutcomeEvent domainEvent, CancellationToken cancellationToken = default);
}
