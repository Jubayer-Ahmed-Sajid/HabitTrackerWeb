using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTrackerWeb.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IHabitLoggingService _habitLoggingService;
    private readonly IRewardService _rewardService;
    private readonly INotificationService _notificationService;
    private readonly IPsychologyTipsService _psychologyTipsService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDashboardService dashboardService,
        IHabitLoggingService habitLoggingService,
        IRewardService rewardService,
        INotificationService notificationService,
        IPsychologyTipsService psychologyTipsService,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _habitLoggingService = habitLoggingService;
        _rewardService = rewardService;
        _notificationService = notificationService;
        _psychologyTipsService = psychologyTipsService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? state, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var snapshot = await _dashboardService.GetDailyDashboardAsync(
            userId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        var rewardSnapshot = await _rewardService.GetRewardSnapshotAsync(userId, snapshot.Date, cancellationToken);
        var notifications = await _notificationService.GetNotificationsAsync(userId, snapshot.Date, cancellationToken);
        var tipsSnapshot = _psychologyTipsService.GetTips(state);

        var vm = new HabitDashboardViewModel
        {
            Date = snapshot.Date,
            TotalHabits = snapshot.TotalHabits,
            CompletedHabits = snapshot.CompletedHabits,
            CompletionPercent = snapshot.CompletionPercent,
            HotStreakHabits = snapshot.HotStreakHabits,
            RewardSummary = new RewardSummaryViewModel
            {
                CurrentLevel = rewardSnapshot.CurrentLevel,
                TotalPoints = rewardSnapshot.TotalPoints,
                NextLevelPoints = rewardSnapshot.NextLevelPoints,
                PointsToNextLevel = rewardSnapshot.PointsToNextLevel,
                LevelProgressPercent = rewardSnapshot.LevelProgressPercent,
                MotivationalLine = rewardSnapshot.MotivationalLine,
                RecentBadges = rewardSnapshot.RecentBadges.Select(b => new RewardBadgeViewModel
                {
                    Title = b.Title,
                    HabitTitle = b.HabitTitle,
                    AwardedAtUtc = b.AwardedAtUtc,
                    Points = b.Points
                }).ToList(),
                UpcomingMilestones = rewardSnapshot.UpcomingMilestones.Select(m => new RewardMilestoneViewModel
                {
                    TargetStreak = m.TargetStreak,
                    Title = m.Title,
                    BonusPoints = m.BonusPoints
                }).ToList()
            },
            Notifications = notifications.Select(n => new DashboardNotificationViewModel
            {
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString().ToLowerInvariant(),
                ActionLabel = n.ActionLabel,
                ActionUrl = n.ActionUrl
            }).ToList(),
            SelectedMentalState = tipsSnapshot.SelectedState,
            PsychologyIntroLine = tipsSnapshot.IntroLine,
            MentalStateOptions = tipsSnapshot.AvailableStates,
            PsychologicalTips = tipsSnapshot.Tips.Select(t => new DashboardPsychologicalTipViewModel
            {
                Title = t.Title,
                Objective = t.Objective,
                WhyItWorks = t.WhyItWorks,
                ActionPrompt = t.ActionPrompt,
                IfThenPlan = t.IfThenPlan,
                RecoveryStep = t.RecoveryStep,
                ReflectionPrompt = t.ReflectionPrompt,
                TimeWindow = t.TimeWindow,
                Steps = t.Steps
            }).ToList(),
            CategoryProgress = snapshot.CategoryProgress.Select(c => new DashboardCategoryProgressViewModel
            {
                CategoryName = c.CategoryName,
                Completed = c.Completed,
                Total = c.Total,
                CompletionPercent = c.CompletionPercent
            }).ToList(),
            RecentAchievements = snapshot.RecentAchievements.Select(a => new DashboardAchievementViewModel
            {
                Title = a.Title,
                HabitTitle = a.HabitTitle,
                AwardedAtUtc = a.AwardedAtUtc
            }).ToList(),
            Habits = snapshot.Habits.Select(h => new DashboardHabitItemViewModel
            {
                HabitId = h.HabitId,
                Title = h.Title,
                Description = h.Description,
                CategoryName = h.CategoryName,
                CompletedToday = h.CompletedToday,
                CurrentStreak = h.CurrentStreak,
                ShowFireIcon = h.ShowFire
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Log([FromForm] LogHabitRequestViewModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var userId = _currentUserService.GetRequiredUserId();
        var result = await _habitLoggingService.LogHabitCompletionAsync(
            request.HabitId,
            userId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        if (!result.Success)
        {
            return Conflict(new
            {
                success = false,
                message = result.Message,
                alreadyLogged = result.AlreadyLogged,
                streak = result.CurrentStreak
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            streak = result.CurrentStreak
        });
    }
}
