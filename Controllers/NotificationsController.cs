using System.Security.Cryptography;
using System.Text;
using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTrackerWeb.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Poll(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var notifications = await _notificationService.GetNotificationsAsync(userId, date, cancellationToken);

        var items = notifications
            .Select(n => new
            {
                key = ComputeKey(n.Title, n.Message, n.Type.ToString(), n.ActionUrl),
                title = n.Title,
                message = n.Message,
                type = n.Type.ToString().ToLowerInvariant(),
                actionLabel = n.ActionLabel,
                actionUrl = n.ActionUrl
            })
            .ToList();

        var signatureSource = string.Join("|", items.Select(i => i.key));
        var signature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signatureSource)));

        return Ok(new
        {
            generatedAtUtc = DateTime.UtcNow,
            unreadCount = items.Count,
            items,
            signature
        });
    }

    private static string ComputeKey(string title, string message, string type, string actionUrl)
    {
        var source = $"{title}|{message}|{type}|{actionUrl}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
