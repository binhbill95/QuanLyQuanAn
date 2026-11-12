using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Settings;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public class SupportSettingsController : Controller
{
    private readonly ISupportContactSettingsService _settingsService;

    public SupportSettingsController(ISupportContactSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        return View(new SupportContactSettingsViewModel
        {
            HotlineNumber = settings.HotlineNumber,
            ZaloUrl = settings.ZaloUrl,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        SupportContactSettingsViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _settingsService.SaveAsync(
            new SupportContactSettingsDto(
                model.HotlineNumber?.Trim() ?? string.Empty,
                model.ZaloUrl?.Trim() ?? string.Empty),
            cancellationToken);

        TempData["SuccessMessage"] = "Đã lưu thông tin liên hệ hỗ trợ.";
        return RedirectToAction(nameof(Index));
    }
}
