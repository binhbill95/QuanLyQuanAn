using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Shifts;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public class ShiftsController : Controller
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var shifts = await _shiftService.GetAllAsync(true, cancellationToken);
        return View(shifts);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ShiftFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ShiftFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _shiftService.CreateAsync(
            new ShiftInput(model.Name, model.StartTime, model.EndTime, model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm ca làm.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã thêm ca {model.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var input = await _shiftService.GetInputAsync(id, cancellationToken);

        if (input is null)
        {
            return NotFound();
        }

        return View(new ShiftFormViewModel
        {
            Id = id,
            Name = input.Name,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            IsActive = input.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ShiftFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _shiftService.UpdateAsync(
            id,
            new ShiftInput(model.Name, model.StartTime, model.EndTime, model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật ca làm.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật ca {model.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var result = await _shiftService.SetActiveAsync(id, isActive, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? (isActive ? "Đã kích hoạt ca làm." : "Đã ngừng sử dụng ca làm.")
                : result.Error;

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Assignments(
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _shiftService.GetAssignmentsAsync(keyword, cancellationToken);

        ViewData["Keyword"] = keyword;

        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> Assign(int id, CancellationToken cancellationToken = default)
    {
        var assignments = await _shiftService.GetAssignmentsAsync(null, cancellationToken);
        var assignment = assignments.FirstOrDefault(a => a.EmployeeId == id);

        if (assignment is null)
        {
            return NotFound();
        }

        var shifts = await _shiftService.GetAllAsync(true, cancellationToken);

        var model = new AssignShiftsViewModel
        {
            EmployeeId = assignment.EmployeeId,
            EmployeeCode = assignment.EmployeeCode,
            EmployeeName = assignment.FullName,
            PositionName = assignment.PositionName,
            Shifts = shifts
                .Select(s => new ShiftCheckboxItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Selected = assignment.ShiftIds.Contains(s.Id),
                })
                .ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        int id,
        List<int>? shiftIds,
        CancellationToken cancellationToken = default)
    {
        var result = await _shiftService.SetEmployeeShiftsAsync(
            id,
            shiftIds ?? [],
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error ?? "Không thể phân ca.";
        }
        else
        {
            TempData["SuccessMessage"] = "Đã cập nhật ca làm của nhân viên.";
        }

        return RedirectToAction(nameof(Assignments));
    }
}
