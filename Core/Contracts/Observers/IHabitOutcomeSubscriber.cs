using HabitTrackerWeb.Core.DomainEvents;

namespace HabitTrackerWeb.Core.Contracts.Observers;

public interface IHabitOutcomeSubscriber
{
    Task HandleOutcomeAsync(HabitOutcomeEvent domainEvent, CancellationToken cancellationToken = default);
}
