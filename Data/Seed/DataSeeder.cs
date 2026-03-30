using HabitTrackerWeb.Core.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Data.Seed;

public class DataSeeder : IDataSeeder
{
    private const string DemoEmail = "demo@habittracker.local";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DataSeeder(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await UpgradeSchemaAsync(cancellationToken);

        await RemoveLegacyDemoDataAsync(cancellationToken);

        if (!await _dbContext.Categories.AnyAsync(cancellationToken))
        {
            var categories = new[]
            {
                new Category { Name = "Health", Description = "Fitness, sleep, and physical energy habits" },
                new Category { Name = "Learning", Description = "Skill growth and knowledge building" },
                new Category { Name = "Productivity", Description = "Execution, planning, and deep work" },
                new Category { Name = "Mindset", Description = "Reflection, gratitude, and emotional resilience" }
            };

            await _dbContext.Categories.AddRangeAsync(categories, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpgradeSchemaAsync(CancellationToken cancellationToken)
    {
        await TryExecuteSqlAsync(
            "ALTER TABLE Habits ADD COLUMN ExternalSource INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);
        await TryExecuteSqlAsync(
            "ALTER TABLE Habits ADD COLUMN ExternalMatchKey TEXT NULL;",
            cancellationToken);
        await TryExecuteSqlAsync(
            "ALTER TABLE Habits ADD COLUMN AutoCompleteFromExternal INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS ExternalAccountLinks (
                Id INTEGER NOT NULL CONSTRAINT PK_ExternalAccountLinks PRIMARY KEY AUTOINCREMENT,
                ApplicationUserId TEXT NOT NULL,
                Source INTEGER NOT NULL,
                ExternalUserName TEXT NOT NULL,
                AccessToken TEXT NULL,
                IsActive INTEGER NOT NULL,
                LastSyncedAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CONSTRAINT FK_ExternalAccountLinks_AspNetUsers_ApplicationUserId
                    FOREIGN KEY (ApplicationUserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
            );",
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS EloRatings (
                Id INTEGER NOT NULL CONSTRAINT PK_EloRatings PRIMARY KEY AUTOINCREMENT,
                ApplicationUserId TEXT NOT NULL,
                CurrentRating INTEGER NOT NULL,
                PeakRating INTEGER NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                CONSTRAINT FK_EloRatings_AspNetUsers_ApplicationUserId
                    FOREIGN KEY (ApplicationUserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
            );",
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS EloRatingChanges (
                Id INTEGER NOT NULL CONSTRAINT PK_EloRatingChanges PRIMARY KEY AUTOINCREMENT,
                ApplicationUserId TEXT NOT NULL,
                HabitId INTEGER NULL,
                OccurredDate TEXT NOT NULL,
                OutcomeType INTEGER NOT NULL,
                DifficultyBasisStreak INTEGER NOT NULL,
                DifficultyMultiplier REAL NOT NULL,
                Delta INTEGER NOT NULL,
                RatingAfterChange INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CONSTRAINT FK_EloRatingChanges_AspNetUsers_ApplicationUserId
                    FOREIGN KEY (ApplicationUserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                CONSTRAINT FK_EloRatingChanges_Habits_HabitId
                    FOREIGN KEY (HabitId) REFERENCES Habits (Id) ON DELETE SET NULL
            );",
            cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_ExternalAccountLinks_AppUser_Source_UserName ON ExternalAccountLinks (ApplicationUserId, Source, ExternalUserName);",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_EloRatings_ApplicationUserId ON EloRatings (ApplicationUserId);",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_EloRatingChanges_User_Habit_Date_Outcome ON EloRatingChanges (ApplicationUserId, HabitId, OccurredDate, OutcomeType);",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_EloRatingChanges_User_Date ON EloRatingChanges (ApplicationUserId, OccurredDate);",
            cancellationToken);
    }

    private async Task TryExecuteSqlAsync(string sql, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1
                                         && ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
            // Column is already present in upgraded databases.
        }
    }

    private async Task RemoveLegacyDemoDataAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(DemoEmail);
        if (user is null)
        {
            return;
        }

        var habitIds = await _dbContext.Habits
            .Where(h => h.ApplicationUserId == user.Id)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);

        if (habitIds.Count > 0)
        {
            var achievements = await _dbContext.Achievements
                .Where(a => a.ApplicationUserId == user.Id || habitIds.Contains(a.HabitId))
                .ToListAsync(cancellationToken);
            if (achievements.Count > 0)
            {
                _dbContext.Achievements.RemoveRange(achievements);
            }

            var logs = await _dbContext.HabitLogs
                .Where(l => l.ApplicationUserId == user.Id || habitIds.Contains(l.HabitId))
                .ToListAsync(cancellationToken);
            if (logs.Count > 0)
            {
                _dbContext.HabitLogs.RemoveRange(logs);
            }

            var metrics = await _dbContext.HabitMetrics
                .Where(m => habitIds.Contains(m.HabitId))
                .ToListAsync(cancellationToken);
            if (metrics.Count > 0)
            {
                _dbContext.HabitMetrics.RemoveRange(metrics);
            }

            var habits = await _dbContext.Habits
                .Where(h => h.ApplicationUserId == user.Id)
                .ToListAsync(cancellationToken);
            if (habits.Count > 0)
            {
                _dbContext.Habits.RemoveRange(habits);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not remove demo user: {errors}");
        }
    }
}
