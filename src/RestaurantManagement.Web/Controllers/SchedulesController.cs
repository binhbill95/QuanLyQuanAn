using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Schedules;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public class SchedulesController : Controller
{
    private readonly IScheduleService _scheduleService;

    public SchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    public async Task<IActionResult> Index(
        DateOnly? from,
        DateOnly? to,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var start = from ?? today;
        var end = to ?? start.AddDays(6);

        if (end < start)
        {
            end = start.AddDays(6);
        }

        var model = new ScheduleIndexViewModel
        {
            From = start,
            To = end,
            EmployeeId = employeeId,
            Schedules = await _scheduleService.GetAsync(start, end, employeeId, cancellationToken),
            Employees = await _scheduleService.GetEmployeeOptionsAsync(null, cancellationToken),
            Shifts = await _scheduleService.GetActiveShiftsAsync(cancellationToken),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int employeeId,
        int shiftId,
        DateOnly workDate,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (employeeId <= 0 || shiftId <= 0 || workDate == default)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn nhân viên, ca làm và ngày.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _scheduleService.CreateAsync(
            new ScheduleInput(employeeId, shiftId, workDate, note),
            cancellationToken);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Đã xếp ca.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "Không thể xếp ca.";
        }

        return RedirectToAction(nameof(Index), new { from = workDate, to = workDate.AddDays(6), employeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        DateOnly? from,
        DateOnly? to,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _scheduleService.DeleteAsync(id, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã xóa lịch ca." : result.Error;

        return RedirectToAction(nameof(Index), new { from, to, employeeId });
    }
}
