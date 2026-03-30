using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.Observers;

public static class EloRatingMath
{
    public static (int Delta, double DifficultyMultiplier) CalculateDelta(
        int currentRating,
        HabitOutcomeType outcomeType,
        int streakBeforeOutcome)
    {
        const int kFactor = 32;

        var difficultyMultiplier = 1d + Math.Clamp(streakBeforeOutcome / 20d, 0d, 2.5d);
        var targetRating = 1000d + (150d * difficultyMultiplier);

        var expected = 1d / (1d + Math.Pow(10d, (targetRating - currentRating) / 400d));
        var actual = outcomeType == HabitOutcomeType.Completed ? 1d : 0d;

        var rawDelta = kFactor * difficultyMultiplier * (actual - expected);
        if (outcomeType == HabitOutcomeType.Missed)
        {
            rawDelta *= 1.10d;
        }

        var rounded = (int)Math.Round(rawDelta, MidpointRounding.AwayFromZero);

        if (outcomeType == HabitOutcomeType.Completed)
        {
            rounded = Math.Max(2, rounded);
        }
        else
        {
            rounded = Math.Min(-2, rounded);
        }

        return (rounded, Math.Round(difficultyMultiplier, 3));
    }
}
