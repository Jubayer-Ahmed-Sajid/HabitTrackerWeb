using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Enums;
using HabitTrackerWeb.ViewModels.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTrackerWeb.Controllers;

[Authorize]
[ApiController]
[Route("api/integrations")]
public sealed class IntegrationsController : ControllerBase
{
    private readonly IExternalIntegrationService _externalIntegrationService;
    private readonly ICurrentUserService _currentUserService;

    public IntegrationsController(
        IExternalIntegrationService externalIntegrationService,
        ICurrentUserService currentUserService)
    {
        _externalIntegrationService = externalIntegrationService;
        _currentUserService = currentUserService;
    }

    [HttpGet("external-links")]
    public async Task<IActionResult> GetExternalLinks(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var links = await _externalIntegrationService.GetExternalLinksAsync(userId, cancellationToken);
        return Ok(links);
    }

    [HttpPost("external-links")]
    public async Task<IActionResult> UpsertExternalLink(
        [FromBody] ExternalAccountLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Source == ExternalActivitySource.None)
        {
            ModelState.AddModelError(nameof(request.Source), "Source must be selected.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = _currentUserService.GetRequiredUserId();

        try
        {
            var result = await _externalIntegrationService.UpsertExternalLinkAsync(
                userId,
                new ExternalAccountLinkUpsertCommand(
                    request.Id,
                    request.Source,
                    request.ExternalUserName,
                    request.AccessToken,
                    request.IsActive),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("external-links/{id:int}")]
    public async Task<IActionResult> DeleteExternalLink(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var deleted = await _externalIntegrationService.DeleteExternalLinkAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("habit-sync")]
    public async Task<IActionResult> GetHabitSyncMappings(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _externalIntegrationService.GetHabitSyncMappingsAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("habits/{habitId:int}/sync")]
    public async Task<IActionResult> UpdateHabitSyncMapping(
        int habitId,
        [FromBody] HabitExternalSyncRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();

        try
        {
            var updated = await _externalIntegrationService.UpdateHabitSyncMappingAsync(
                userId,
                habitId,
                new HabitExternalSyncUpdateCommand(
                    request.AutoCompleteFromExternal,
                    request.ExternalSource,
                    request.ExternalMatchKey),
                cancellationToken);

            if (!updated)
            {
                return NotFound();
            }

            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
