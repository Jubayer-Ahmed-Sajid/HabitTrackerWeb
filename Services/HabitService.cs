using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Services;

public class HabitService : IHabitService
{
    private readonly IUnitOfWork _unitOfWork;

    public HabitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Habit>> GetHabitsForUserAsync(string userId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Habits.GetHabitsForUserAsync(userId, activeOnly, cancellationToken);
    }

    public async Task<Habit?> GetHabitForUserAsync(int habitId, string userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Habits.GetByIdForUserAsync(habitId, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Categories.GetAllOrderedAsync(cancellationToken);
    }

    public async Task<Habit> CreateHabitAsync(string userId, HabitUpsertCommand command, CancellationToken cancellationToken = default)
    {
        var habit = new Habit
        {
            Title = command.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            Frequency = command.Frequency,
            SpecificDays = string.IsNullOrWhiteSpace(command.SpecificDays) ? null : command.SpecificDays.Trim(),
            CategoryId = command.CategoryId,
            ApplicationUserId = userId,
            IsActive = command.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Habits.AddAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return habit;
    }

    public async Task<bool> UpdateHabitAsync(int habitId, string userId, HabitUpsertCommand command, CancellationToken cancellationToken = default)
    {
        var habit = await _unitOfWork.Habits.GetByIdForUserAsync(habitId, userId, cancellationToken);
        if (habit is null)
        {
            return false;
        }

        habit.Title = command.Title.Trim();
        habit.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        habit.Frequency = command.Frequency;
        habit.SpecificDays = string.IsNullOrWhiteSpace(command.SpecificDays) ? null : command.SpecificDays.Trim();
        habit.CategoryId = command.CategoryId;
        habit.IsActive = command.IsActive;

        _unitOfWork.Habits.Update(habit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateHabitAsync(int habitId, string userId, CancellationToken cancellationToken = default)
    {
        var habit = await _unitOfWork.Habits.GetByIdForUserAsync(habitId, userId, cancellationToken);
        if (habit is null)
        {
            return false;
        }

        habit.IsActive = false;
        _unitOfWork.Habits.Update(habit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
