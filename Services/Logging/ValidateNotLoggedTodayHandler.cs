using HabitTrackerWeb.Core.Contracts.Repositories;

namespace HabitTrackerWeb.Services.Logging;

public class ValidateNotLoggedTodayHandler : HabitLogHandlerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ValidateNotLoggedTodayHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleInternalAsync(HabitLogContext context, CancellationToken cancellationToken)
    {
        var alreadyLogged = await _unitOfWork.HabitLogs.ExistsForDateAsync(
            context.HabitId,
            context.UserId,
            context.LoggedDate,
            cancellationToken);

        if (alreadyLogged)
        {
            context.Invalidate("Habit already logged for today.", alreadyLogged: true);
        }
    }
}
