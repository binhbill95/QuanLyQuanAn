using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;

namespace RestaurantManagement.Web.ViewComponents;

public sealed class SupportContactViewComponent : ViewComponent
{
    private readonly ISupportContactSettingsService _settingsService;

    public SupportContactViewComponent(ISupportContactSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        return View(settings);
    }
}
