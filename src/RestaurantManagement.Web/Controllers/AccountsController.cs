using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Accounts;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

/// <summary>Quản lý tài khoản đăng nhập — chỉ ADMIN (docs/03).</summary>
[Authorize(Roles = "ADMIN")]
public class AccountsController : Controller
{
    private const string AdminRoleCode = "ADMIN";

    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUser;

    public AccountsController(IAccountService accountService, ICurrentUserService currentUser)
    {
        _accountService = accountService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        int? roleId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var model = new AccountIndexViewModel
        {
            Accounts = await _accountService.SearchAsync(keyword, roleId, isActive, cancellationToken),
            Roles = await _accountService.GetRoleOptionsAsync(cancellationToken),
            Keyword = keyword,
            RoleId = roleId,
            IsActive = isActive,
            CurrentUserId = _currentUser.UserId ?? 0,
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var model = new AccountCreateViewModel
        {
            Employees = await _accountService.GetEmployeeOptionsAsync(cancellationToken),
            Roles = await GetAssignableRolesAsync(cancellationToken),
        };

        if (employeeId.HasValue && model.Employees.Any(e => e.Id == employeeId.Value))
        {
            model.EmployeeId = employeeId.Value;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AccountCreateViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await ReloadCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _accountService.CreateAsync(
            new CreateAccountInput(model.EmployeeId, model.Username, model.RoleId, model.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo tài khoản.");
            await ReloadCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã tạo tài khoản '{model.Username.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var details = await _accountService.GetDetailsAsync(id, cancellationToken);

        if (details is null)
        {
            return NotFound();
        }

        if (IsAdminAccount(details.RoleCode))
        {
            TempData["ErrorMessage"] = "Tài khoản ADMIN là tài khoản hệ thống, không sửa ở màn hình này.";
            return RedirectToAction(nameof(Index));
        }

        return View(new AccountEditViewModel
        {
            UserId = details.UserId,
            Username = details.Username,
            EmployeeCode = details.EmployeeCode,
            EmployeeName = details.EmployeeName,
            RoleCode = details.RoleCode,
            RoleId = details.RoleId,
            Roles = await GetAssignableRolesAsync(cancellationToken),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AccountEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Roles = await GetAssignableRolesAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.UpdateRoleAsync(id, model.RoleId, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đổi vai trò.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đã đổi vai trò. Người dùng sẽ phải đăng nhập lại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken cancellationToken = default)
    {
        var details = await _accountService.GetDetailsAsync(id, cancellationToken);

        if (details is null)
        {
            return NotFound();
        }

        if (details.UserId == _currentUser.UserId)
        {
            TempData["ErrorMessage"] = "Hãy dùng chức năng Đổi mật khẩu cho tài khoản của bạn.";
            return RedirectToAction(nameof(Index));
        }

        if (IsAdminAccount(details.RoleCode))
        {
            TempData["ErrorMessage"] = "Không thể đặt lại mật khẩu tài khoản ADMIN.";
            return RedirectToAction(nameof(Index));
        }

        return View(new AccountResetPasswordViewModel
        {
            UserId = details.UserId,
            Username = details.Username,
            EmployeeName = details.EmployeeName,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        int id,
        AccountResetPasswordViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.ResetPasswordAsync(id, model.NewPassword, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đặt lại mật khẩu.");
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Đã đặt lại mật khẩu cho '{model.Username}'. Phiên đăng nhập cũ đã bị đăng xuất.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.SetActiveAsync(id, isActive, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? (isActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.")
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<AccountRoleOptionDto>> GetAssignableRolesAsync(
        CancellationToken cancellationToken)
    {
        var roles = await _accountService.GetRoleOptionsAsync(cancellationToken);

        // ADMIN là tài khoản hệ thống, không gán qua màn hình này.
        return roles
            .Where(r => !IsAdminAccount(r.Code))
            .ToList();
    }

    private async Task ReloadCreateOptionsAsync(
        AccountCreateViewModel model,
        CancellationToken cancellationToken)
    {
        model.Employees = await _accountService.GetEmployeeOptionsAsync(cancellationToken);
        model.Roles = await GetAssignableRolesAsync(cancellationToken);
    }

    private static bool IsAdminAccount(string roleCode)
        => string.Equals(roleCode, AdminRoleCode, StringComparison.OrdinalIgnoreCase);
}
