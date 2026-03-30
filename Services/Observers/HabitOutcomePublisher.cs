using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.DomainEvents;

namespace HabitTrackerWeb.Services.Observers;

public sealed class HabitOutcomePublisher : IHabitOutcomePublisher
{
    private readonly IReadOnlyList<IHabitOutcomeSubscriber> _subscribers;

    public HabitOutcomePublisher(IEnumerable<IHabitOutcomeSubscriber> subscribers)
    {
        _subscribers = subscribers.ToList();
    }

    public async Task PublishAsync(HabitOutcomeEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in _subscribers)
        {
            await subscriber.HandleOutcomeAsync(domainEvent, cancellationToken);
        }
    }
}
