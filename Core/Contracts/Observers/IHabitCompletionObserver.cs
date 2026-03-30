using HabitTrackerWeb.Core.DomainEvents;

namespace HabitTrackerWeb.Core.Contracts.Observers;

public interface IHabitCompletionObserver
{
    Task OnHabitCompletedAsync(HabitCompletedEvent habitCompletedEvent, CancellationToken cancellationToken = default);
}
