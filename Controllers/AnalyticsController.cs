using System.Text.Json;
using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.ViewModels.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTrackerWeb.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsController(IAnalyticsService analyticsService, ICurrentUserService currentUserService)
    {
        _analyticsService = analyticsService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var snapshot = await _analyticsService.GetAnalyticsAsync(userId, days, cancellationToken);

        var vm = new AnalyticsViewModel
        {
            SelectedRangeDays = snapshot.RangeDays,
            ActiveHabits = snapshot.Kpi.ActiveHabits,
            TotalCompletions = snapshot.Kpi.TotalCompletions,
            AverageDailyCompletionPercent = snapshot.Kpi.AverageDailyCompletionPercent,
            BestDayLabel = snapshot.Kpi.BestDayLabel,
            BestDayCompletions = snapshot.Kpi.BestDayCompletions,
            HotStreakHabits = snapshot.Kpi.HotStreakHabits,
            CurrentEloRating = snapshot.Kpi.CurrentEloRating,
            PeakEloRating = snapshot.Kpi.PeakEloRating,
            WeeklyLabelsJson = JsonSerializer.Serialize(snapshot.WeeklyConsistency.Select(p => p.Date.ToString("ddd"))),
            WeeklyCompletionJson = JsonSerializer.Serialize(snapshot.WeeklyConsistency.Select(p => p.CompletionPercent)),
            TrendLabelsJson = JsonSerializer.Serialize(snapshot.CompletionTrend.Select(p => p.Date.ToString("MM-dd"))),
            TrendDataJson = JsonSerializer.Serialize(snapshot.CompletionTrend.Select(p => p.CompletedHabits)),
            CategoryLabelsJson = JsonSerializer.Serialize(snapshot.CategoryPerformance.Select(c => c.CategoryName)),
            CategoryCompletionJson = JsonSerializer.Serialize(snapshot.CategoryPerformance.Select(c => c.CompletionPercent)),
            Insights = snapshot.Insights,
            CategoryPerformance = snapshot.CategoryPerformance.Select(c => new CategoryPerformanceViewModel
            {
                CategoryName = c.CategoryName,
                ScheduledCount = c.ScheduledCount,
                CompletedCount = c.CompletedCount,
                CompletionPercent = c.CompletionPercent
            }).ToList(),
            RecentAchievements = snapshot.RecentAchievements.Select(a => new AchievementTimelineItemViewModel
            {
                Title = a.Title,
                HabitTitle = a.HabitTitle,
                AwardedAtUtc = a.AwardedAtUtc
            }).ToList(),
            HabitStats = snapshot.HabitStats.Select(h => new HabitAnalyticsRowViewModel
            {
                HabitTitle = h.HabitTitle,
                CurrentStreak = h.CurrentStreak,
                LongestStreak = h.LongestStreak,
                CompletionRate30Days = h.CompletionRate30Days
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Heatmap([FromQuery] int days = 365, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var points = await _analyticsService.GetContributionHeatmapAsync(userId, days, cancellationToken);

        var response = points.Select(p => new
        {
            date = p.Date.ToString("yyyy-MM-dd"),
            completedHabits = p.CompletedHabits,
            eloPointsGained = p.EloPointsGained,
            intensityLevel = p.IntensityLevel,
            anyCompletion = p.AnyCompletion
        });

        return Ok(response);
    }
}
