using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.ViewModels.Push;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTrackerWeb.Controllers;

[Authorize]
public class PushController : Controller
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ICurrentUserService _currentUserService;

    public PushController(
        IPushNotificationService pushNotificationService,
        ICurrentUserService currentUserService)
    {
        _pushNotificationService = pushNotificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public IActionResult PublicKey()
    {
        if (!_pushNotificationService.IsEnabled)
        {
            return NotFound();
        }

        var key = _pushNotificationService.GetPublicKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            return NotFound();
        }

        return Content(key, "text/plain");
    }

    [HttpGet]
    public async Task<IActionResult> Preference(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var enabled = await _pushNotificationService.GetUserPreferenceAsync(userId, cancellationToken);

        return Ok(new
        {
            enabled,
            available = _pushNotificationService.IsEnabled
        });
    }

    [HttpPost]
    public async Task<IActionResult> Preference([FromBody] PushPreferenceRequest request, CancellationToken cancellationToken)
    {
        if (!_pushNotificationService.IsEnabled)
        {
            return Conflict(new { success = false, message = "Push notifications are currently unavailable." });
        }

        var userId = _currentUserService.GetRequiredUserId();
        await _pushNotificationService.SetUserPreferenceAsync(userId, request.Enabled, cancellationToken);

        return Ok(new { success = true, enabled = request.Enabled });
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid push subscription payload." });
        }

        if (!_pushNotificationService.IsEnabled)
        {
            return Conflict(new { success = false, message = "Push notifications are currently unavailable." });
        }

        var userId = _currentUserService.GetRequiredUserId();
        await _pushNotificationService.SaveSubscriptionAsync(
            userId,
            new PushSubscriptionRegistration(request.Endpoint, request.P256Dh, request.Auth),
            cancellationToken);

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid unsubscribe payload." });
        }

        var userId = _currentUserService.GetRequiredUserId();
        await _pushNotificationService.RemoveSubscriptionAsync(userId, request.Endpoint, cancellationToken);

        return Ok(new { success = true });
    }
}
