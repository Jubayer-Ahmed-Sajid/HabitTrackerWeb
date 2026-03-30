using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Services.Logging;

public class PersistHabitLogHandler : HabitLogHandlerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PersistHabitLogHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleInternalAsync(HabitLogContext context, CancellationToken cancellationToken)
    {
        var log = new HabitLog
        {
            HabitId = context.HabitId,
            ApplicationUserId = context.UserId,
            LoggedDate = context.LoggedDate,
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.HabitLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        context.PersistedLog = log;
    }
}
