using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Employees;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public class EmployeesController : Controller
{
    private const int PageSize = 20;

    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        int? positionId,
        byte? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var employees = await _employeeService.SearchAsync(
            new EmployeeSearchRequest(keyword, positionId, status, page, PageSize),
            cancellationToken);

        var model = new EmployeeIndexViewModel
        {
            Employees = employees,
            Keyword = keyword,
            PositionId = positionId,
            Status = status,
            Positions = await _employeeService.GetPositionsAsync(cancellationToken),
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeService.GetDetailsAsync(id, cancellationToken);

        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var model = new EmployeeFormViewModel
        {
            EmployeeCode = await _employeeService.SuggestEmployeeCodeAsync(cancellationToken),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = 0,
        };

        await PopulateOptionsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        EmployeeFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _employeeService.CreateAsync(ToInput(model), cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm nhân viên.");
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã thêm nhân viên {model.FullName} ({model.EmployeeCode}).";

        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var input = await _employeeService.GetInputAsync(id, cancellationToken);

        if (input is null)
        {
            return NotFound();
        }

        var model = ToForm(input);
        model.Id = id;

        await PopulateOptionsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EmployeeFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _employeeService.UpdateAsync(id, ToInput(model), cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật nhân viên.");
            await PopulateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật nhân viên {model.FullName}.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.SoftDeleteAsync(id, cancellationToken);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Đã xóa nhân viên (xóa mềm).";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "Không thể xóa nhân viên.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(EmployeeFormViewModel model, CancellationToken cancellationToken)
    {
        var positions = await _employeeService.GetPositionsAsync(cancellationToken);

        model.PositionOptions = positions
            .Select(p => new SelectListItem(
                p.IsActive ? p.Name : $"{p.Name} (ngừng hoạt động)",
                p.Id.ToString()))
            .ToList();
    }

    private static EmployeeInput ToInput(EmployeeFormViewModel model) => new(
        model.EmployeeCode,
        model.FullName,
        model.Gender,
        model.DateOfBirth,
        model.PositionId,
        model.Phone,
        model.Address,
        model.IdentityNumber,
        model.StartDate,
        model.EndDate,
        model.ShiftRate,
        model.Status,
        model.Note);

    private static EmployeeFormViewModel ToForm(EmployeeInput input) => new()
    {
        EmployeeCode = input.EmployeeCode,
        FullName = input.FullName,
        Gender = input.Gender,
        DateOfBirth = input.DateOfBirth,
        PositionId = input.PositionId,
        Phone = input.Phone,
        Address = input.Address,
        IdentityNumber = input.IdentityNumber,
        StartDate = input.StartDate,
        EndDate = input.EndDate,
        ShiftRate = input.ShiftRate,
        Status = input.Status,
        Note = input.Note,
    };
}
