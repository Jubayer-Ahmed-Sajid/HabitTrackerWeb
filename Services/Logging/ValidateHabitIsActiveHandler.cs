using HabitTrackerWeb.Core.Contracts.Repositories;

namespace HabitTrackerWeb.Services.Logging;

public class ValidateHabitIsActiveHandler : HabitLogHandlerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ValidateHabitIsActiveHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleInternalAsync(HabitLogContext context, CancellationToken cancellationToken)
    {
        var habit = await _unitOfWork.Habits.GetByIdForUserAsync(context.HabitId, context.UserId, cancellationToken);

        if (habit is null)
        {
            context.Invalidate("Habit not found.");
            return;
        }

        if (!habit.IsActive)
        {
            context.Invalidate("Habit is inactive and cannot be logged.");
            return;
        }

        context.Habit = habit;
    }
}
