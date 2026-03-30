using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.ViewModels.Habits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HabitTrackerWeb.Controllers;

[Authorize]
public class HabitsController : Controller
{
    private readonly IHabitService _habitService;
    private readonly ICurrentUserService _currentUserService;

    public HabitsController(IHabitService habitService, ICurrentUserService currentUserService)
    {
        _habitService = habitService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var habits = await _habitService.GetHabitsForUserAsync(userId, activeOnly: false, cancellationToken);

        var vm = new HabitListViewModel
        {
            Habits = habits.Select(h => new HabitListItemViewModel
            {
                Id = h.Id,
                Title = h.Title,
                Description = h.Description,
                Frequency = h.Frequency.ToString(),
                CategoryName = h.Category?.Name ?? "Uncategorized",
                IsActive = h.IsActive,
                CurrentStreak = h.HabitMetric?.CurrentStreak ?? 0
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var vm = new HabitFormViewModel();
        await PopulateCategoriesAsync(vm, cancellationToken);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HabitFormViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm, cancellationToken);
            return View(vm);
        }

        var userId = _currentUserService.GetRequiredUserId();
        await _habitService.CreateHabitAsync(
            userId,
            new HabitUpsertCommand(
                vm.Title,
                vm.Description,
                vm.Frequency,
                vm.SpecificDays,
                vm.CategoryId,
                vm.IsActive),
            cancellationToken);

        TempData["StatusMessage"] = "Habit created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var habit = await _habitService.GetHabitForUserAsync(id, userId, cancellationToken);
        if (habit is null)
        {
            return NotFound();
        }

        var vm = new HabitFormViewModel
        {
            Id = habit.Id,
            Title = habit.Title,
            Description = habit.Description,
            Frequency = habit.Frequency,
            SpecificDays = habit.SpecificDays,
            CategoryId = habit.CategoryId,
            IsActive = habit.IsActive
        };

        await PopulateCategoriesAsync(vm, cancellationToken);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HabitFormViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm, cancellationToken);
            return View(vm);
        }

        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _habitService.UpdateHabitAsync(
            id,
            userId,
            new HabitUpsertCommand(
                vm.Title,
                vm.Description,
                vm.Frequency,
                vm.SpecificDays,
                vm.CategoryId,
                vm.IsActive),
            cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Habit updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _habitService.DeactivateHabitAsync(id, userId, cancellationToken);
        TempData["StatusMessage"] = "Habit archived.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(HabitFormViewModel vm, CancellationToken cancellationToken)
    {
        var categories = await _habitService.GetCategoriesAsync(cancellationToken);
        vm.CategoryOptions = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
    }
}
