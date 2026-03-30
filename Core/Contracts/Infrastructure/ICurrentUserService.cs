namespace HabitTrackerWeb.Core.Contracts.Infrastructure;

public interface ICurrentUserService
{
    string GetRequiredUserId();
}
