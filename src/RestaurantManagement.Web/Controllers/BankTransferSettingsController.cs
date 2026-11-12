using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Payments;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class BankTransferSettingsController : Controller
{
    private readonly IBankTransferSettingsService _settingsService;

    public BankTransferSettingsController(IBankTransferSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        return View(new BankTransferSettingsViewModel
        {
            BankCode = settings.BankCode,
            BankName = settings.BankName,
            AccountNumber = settings.AccountNumber,
            AccountName = settings.AccountName,
            Branch = settings.Branch,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        BankTransferSettingsViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _settingsService.SaveAsync(
            new BankTransferSettingsDto(
                model.BankCode,
                model.BankName,
                model.AccountNumber,
                model.AccountName,
                model.Branch),
            cancellationToken);

        TempData["SuccessMessage"] = "Đã lưu thông tin tài khoản nhận chuyển khoản.";
        return RedirectToAction(nameof(Index));
    }
}
