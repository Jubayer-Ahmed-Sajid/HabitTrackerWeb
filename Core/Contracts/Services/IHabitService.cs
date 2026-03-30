using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IHabitService
{
    Task<IReadOnlyList<Habit>> GetHabitsForUserAsync(string userId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<Habit?> GetHabitForUserAsync(int habitId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Habit> CreateHabitAsync(string userId, HabitUpsertCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateHabitAsync(int habitId, string userId, HabitUpsertCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeactivateHabitAsync(int habitId, string userId, CancellationToken cancellationToken = default);
}
