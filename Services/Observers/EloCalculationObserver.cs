using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.DomainEvents;
using HabitTrackerWeb.Core.Entities;

namespace HabitTrackerWeb.Services.Observers;

public sealed class EloCalculationObserver : IEloCalculationObserver
{
    private readonly IUnitOfWork _unitOfWork;

    public EloCalculationObserver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task HandleOutcomeAsync(HabitOutcomeEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var alreadyApplied = await _unitOfWork.EloRatingChanges.ExistsForOutcomeAsync(
            domainEvent.UserId,
            domainEvent.HabitId,
            domainEvent.Date,
            domainEvent.OutcomeType,
            cancellationToken);

        if (alreadyApplied)
        {
            return;
        }

        var rating = await _unitOfWork.EloRatings.GetForUserAsync(domainEvent.UserId, cancellationToken);
        var isNewRating = rating is null;

        if (rating is null)
        {
            rating = new EloRating
            {
                ApplicationUserId = domainEvent.UserId,
                CurrentRating = 1000,
                PeakRating = 1000,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.EloRatings.AddAsync(rating, cancellationToken);
        }

        var (delta, multiplier) = EloRatingMath.CalculateDelta(
            currentRating: rating.CurrentRating,
            outcomeType: domainEvent.OutcomeType,
            streakBeforeOutcome: domainEvent.StreakBeforeOutcome);

        var newRating = Math.Clamp(rating.CurrentRating + delta, 600, 3000);
        rating.CurrentRating = newRating;
        rating.PeakRating = Math.Max(rating.PeakRating, newRating);
        rating.UpdatedAtUtc = DateTime.UtcNow;

        if (!isNewRating)
        {
            _unitOfWork.EloRatings.Update(rating);
        }

        await _unitOfWork.EloRatingChanges.AddAsync(new EloRatingChange
        {
            ApplicationUserId = domainEvent.UserId,
            HabitId = domainEvent.HabitId,
            OccurredDate = domainEvent.Date,
            OutcomeType = domainEvent.OutcomeType,
            DifficultyBasisStreak = domainEvent.StreakBeforeOutcome,
            DifficultyMultiplier = multiplier,
            Delta = delta,
            RatingAfterChange = newRating,
            Reason = domainEvent.Reason,
            CreatedAtUtc = domainEvent.OccurredAtUtc
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
