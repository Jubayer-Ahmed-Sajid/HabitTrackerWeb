namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IRewardService
{
    Task<RewardSnapshot> GetRewardSnapshotAsync(
        string userId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}
