using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.DomainEvents;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Services.Logging;

namespace HabitTrackerWeb.Services;

public class HabitLoggingService : IHabitLoggingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHabitLogHandler _pipelineStart;
    private readonly IStreakCalculatorObserver _streakObserver;
    private readonly IAchievementObserver _achievementObserver;
    private readonly IHabitOutcomePublisher _habitOutcomePublisher;

    public HabitLoggingService(
        IUnitOfWork unitOfWork,
        ValidateHabitIsActiveHandler validateHabitIsActiveHandler,
        ValidateNotLoggedTodayHandler validateNotLoggedTodayHandler,
        PersistHabitLogHandler persistHabitLogHandler,
        IStreakCalculatorObserver streakObserver,
        IAchievementObserver achievementObserver,
        IHabitOutcomePublisher? habitOutcomePublisher = null)
    {
        _unitOfWork = unitOfWork;
        _streakObserver = streakObserver;
        _achievementObserver = achievementObserver;
        _habitOutcomePublisher = habitOutcomePublisher ?? NoOpHabitOutcomePublisher.Instance;

        validateHabitIsActiveHandler
            .SetNext(validateNotLoggedTodayHandler)
            .SetNext(persistHabitLogHandler);

        _pipelineStart = validateHabitIsActiveHandler;
    }

    public async Task<HabitLogResult> LogHabitCompletionAsync(
        int habitId,
        string userId,
        DateOnly loggedDate,
        CancellationToken cancellationToken = default)
    {
        var context = new HabitLogContext
        {
            HabitId = habitId,
            UserId = userId,
            LoggedDate = loggedDate
        };

        await _pipelineStart.HandleAsync(context, cancellationToken);

        if (!context.IsValid)
        {
            return new HabitLogResult(
                Success: false,
                AlreadyLogged: context.AlreadyLogged,
                CurrentStreak: context.Habit?.HabitMetric?.CurrentStreak ?? 0,
                Message: context.Message);
        }

        var domainEvent = new HabitCompletedEvent(
            HabitId: habitId,
            UserId: userId,
            LoggedDate: loggedDate,
            OccurredAtUtc: DateTime.UtcNow);

        await _streakObserver.OnHabitCompletedAsync(domainEvent, cancellationToken);
        await _achievementObserver.OnHabitCompletedAsync(domainEvent, cancellationToken);

        var metric = await _unitOfWork.HabitMetrics.GetByHabitIdAsync(habitId, cancellationToken);
        var streak = metric?.CurrentStreak ?? 0;

        await _habitOutcomePublisher.PublishAsync(
            new HabitOutcomeEvent(
                HabitId: habitId,
                UserId: userId,
                Date: loggedDate,
                OutcomeType: HabitOutcomeType.Completed,
                StreakBeforeOutcome: Math.Max(streak - 1, 0),
                StreakAfterOutcome: streak,
                OccurredAtUtc: DateTime.UtcNow,
                Reason: "Habit completion logged"),
            cancellationToken);

        return new HabitLogResult(
            Success: true,
            AlreadyLogged: false,
            CurrentStreak: streak,
            Message: "Habit completed. Keep the streak alive.");
    }

    private sealed class NoOpHabitOutcomePublisher : IHabitOutcomePublisher
    {
        public static readonly NoOpHabitOutcomePublisher Instance = new();

        private NoOpHabitOutcomePublisher()
        {
        }

        public Task PublishAsync(HabitOutcomeEvent domainEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
