namespace HabitTrackerWeb.Core.Contracts.Services;

public interface IPsychologyTipsService
{
    PsychologyTipsSnapshot GetTips(string? state);
}
