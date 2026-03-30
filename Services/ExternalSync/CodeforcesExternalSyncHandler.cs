using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.ExternalSync;

public sealed class CodeforcesExternalSyncHandler : ExternalSyncHandlerBase
{
    private readonly IHabitLoggingService _habitLoggingService;

    public CodeforcesExternalSyncHandler(IHabitLoggingService habitLoggingService)
    {
        _habitLoggingService = habitLoggingService;
    }

    public override int Order => 30;

    protected override async Task HandleCurrentAsync(ExternalSyncContext context, CancellationToken cancellationToken)
    {
        var items = context.ActivityItems
            .Where(x => x.Source == ExternalActivitySource.Codeforces && x.ActivityDate == context.Date && x.Quantity > 0)
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        context.DataItemsProcessed += items.Sum(x => x.Quantity);

        var habits = context.AutoSyncHabits
            .Where(h => h.AutoCompleteFromExternal && h.ExternalSource == ExternalActivitySource.Codeforces)
            .ToList();

        foreach (var habit in habits)
        {
            var hasMatch = string.IsNullOrWhiteSpace(habit.ExternalMatchKey)
                ? items.Count > 0
                : items.Any(x => string.Equals(x.MatchKey, habit.ExternalMatchKey, StringComparison.OrdinalIgnoreCase));

            if (!hasMatch)
            {
                continue;
            }

            var result = await _habitLoggingService.LogHabitCompletionAsync(
                habit.Id,
                context.UserId,
                context.Date,
                cancellationToken);

            if (result.Success)
            {
                context.HabitsAutoCompleted++;
            }
        }
    }
}
