using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.Data;
using HabitTrackerWeb.Repositories;
using HabitTrackerWeb.Services;
using HabitTrackerWeb.Services.Logging;
using HabitTrackerWeb.Services.Observers;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Tests;

public class HabitLoggingServiceTests
{
    [Fact]
    public async Task LogHabitCompletionAsync_ShouldPersistLogAndUpdateStreakAndAchievements()
    {
        await using var context = CreateContext();

        const string userId = "user-1";
        var habit = await SeedHabitAsync(context, userId, isActive: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        await context.HabitLogs.AddRangeAsync(
            new HabitLog { HabitId = habit.Id, ApplicationUserId = userId, LoggedDate = today.AddDays(-2) },
            new HabitLog { HabitId = habit.Id, ApplicationUserId = userId, LoggedDate = today.AddDays(-1) });
        await context.SaveChangesAsync();

        var service = CreateLoggingService(context);

        var result = await service.LogHabitCompletionAsync(habit.Id, userId, today);

        Assert.True(result.Success);
        Assert.Equal(3, result.CurrentStreak);

        var metric = await context.HabitMetrics.SingleAsync(m => m.HabitId == habit.Id);
        Assert.Equal(3, metric.CurrentStreak);
        Assert.Equal(3, metric.LongestStreak);

        var achievements = await context.Achievements.Where(a => a.HabitId == habit.Id).ToListAsync();
        Assert.Contains(achievements, a => a.Type == AchievementType.Streak3);
    }

    [Fact]
    public async Task LogHabitCompletionAsync_ShouldRejectWhenAlreadyLoggedToday()
    {
        await using var context = CreateContext();

        const string userId = "user-2";
        var habit = await SeedHabitAsync(context, userId, isActive: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        await context.HabitLogs.AddAsync(new HabitLog
        {
            HabitId = habit.Id,
            ApplicationUserId = userId,
            LoggedDate = today
        });
        await context.SaveChangesAsync();

        var service = CreateLoggingService(context);

        var result = await service.LogHabitCompletionAsync(habit.Id, userId, today);

        Assert.False(result.Success);
        Assert.True(result.AlreadyLogged);

        var logCount = await context.HabitLogs.CountAsync(l => l.HabitId == habit.Id && l.ApplicationUserId == userId);
        Assert.Equal(1, logCount);
    }

    [Fact]
    public async Task LogHabitCompletionAsync_ShouldRejectInactiveHabit()
    {
        await using var context = CreateContext();

        const string userId = "user-3";
        var habit = await SeedHabitAsync(context, userId, isActive: false);
        var service = CreateLoggingService(context);

        var result = await service.LogHabitCompletionAsync(habit.Id, userId, DateOnly.FromDateTime(DateTime.UtcNow.Date));

        Assert.False(result.Success);
        Assert.Contains("inactive", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"HabitTrackerTests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<Habit> SeedHabitAsync(ApplicationDbContext context, string userId, bool isActive)
    {
        var category = new Category { Name = $"Category-{Guid.NewGuid():N}" };
        await context.Categories.AddAsync(category);

        var habit = new Habit
        {
            Title = "Test Habit",
            Category = category,
            ApplicationUserId = userId,
            Frequency = HabitFrequency.Daily,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        await context.Habits.AddAsync(habit);
        await context.SaveChangesAsync();

        return habit;
    }

    private static HabitLoggingService CreateLoggingService(ApplicationDbContext context)
    {
        var categoryRepository = new CategoryRepository(context);
        var habitRepository = new HabitRepository(context);
        var habitLogRepository = new HabitLogRepository(context);
        var habitMetricRepository = new HabitMetricRepository(context);
        var achievementRepository = new AchievementRepository(context);

        var unitOfWork = new UnitOfWork(
            context,
            categoryRepository,
            habitRepository,
            habitLogRepository,
            habitMetricRepository,
            achievementRepository);

        var validateActive = new ValidateHabitIsActiveHandler(unitOfWork);
        var validateNotLogged = new ValidateNotLoggedTodayHandler(unitOfWork);
        var persistHandler = new PersistHabitLogHandler(unitOfWork);

        var streakObserver = new StreakCalculatorObserver(unitOfWork);
        var achievementObserver = new AchievementObserver(unitOfWork);

        return new HabitLoggingService(
            unitOfWork,
            validateActive,
            validateNotLogged,
            persistHandler,
            streakObserver,
            achievementObserver);
    }
}
