using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Attendance;
using RestaurantManagement.Application.Models.Shifts;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IScheduleService _scheduleService;
    private readonly ICurrentUserService _currentUser;

    public AttendanceController(
        IAttendanceService attendanceService,
        IScheduleService scheduleService,
        ICurrentUserService currentUser)
    {
        _attendanceService = attendanceService;
        _scheduleService = scheduleService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        DateOnly? date,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Now);

        var isManagement =
            User.IsInRole("ADMIN") || User.IsInRole("OWNER") || User.IsInRole("MANAGER");

        var currentEmployeeId = _currentUser.EmployeeId;
        var hasEmployeeProfile = currentEmployeeId.HasValue;

        string? notice = null;
        IReadOnlyList<AttendanceRowDto> rows = [];
        IReadOnlyList<EmployeeLookupDto> employees = [];

        if (isManagement)
        {
            rows = await _attendanceService.GetAsync(selectedDate, employeeId, cancellationToken);
            employees = await _scheduleService.GetEmployeeOptionsAsync(null, cancellationToken);
        }
        else if (hasEmployeeProfile)
        {
            rows = await _attendanceService.GetAsync(selectedDate, currentEmployeeId, cancellationToken);
        }
        else
        {
            notice = "Tài khoản của bạn chưa được gắn hồ sơ nhân viên nên không thể chấm công.";
        }

        var model = new AttendanceIndexViewModel
        {
            Date = selectedDate,
            EmployeeId = employeeId,
            Rows = rows,
            Employees = employees,
            IsManagement = isManagement,
            HasEmployeeProfile = hasEmployeeProfile,
            CurrentEmployeeId = currentEmployeeId,
            Notice = notice,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(
        int scheduleId,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var employeeId = _currentUser.EmployeeId;

        if (employeeId is null)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { date });
        }

        var result = await _attendanceService.CheckInAsync(scheduleId, employeeId.Value, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã check-in." : result.Error;

        return RedirectToAction(nameof(Index), new { date });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(
        int attendanceId,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var employeeId = _currentUser.EmployeeId;

        if (employeeId is null)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { date });
        }

        var result = await _attendanceService.CheckOutAsync(attendanceId, employeeId.Value, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã check-out." : result.Error;

        return RedirectToAction(nameof(Index), new { date });
    }

    [HttpPost]
    [Authorize(Policy = "Management")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int attendanceId,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var result = await _attendanceService.ApproveAsync(
            attendanceId,
            _currentUser.EmployeeId,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã duyệt công." : result.Error;

        return RedirectToAction(nameof(Index), new { date });
    }
}
