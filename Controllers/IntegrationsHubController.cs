using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTrackerWeb.Controllers;

[Authorize]
[Route("Integrations")]
public sealed class IntegrationsHubController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
