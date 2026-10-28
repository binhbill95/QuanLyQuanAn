using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Audit;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public sealed class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(
        AuditFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.FromDate.HasValue &&
            filter.ToDate.HasValue &&
            filter.FromDate > filter.ToDate)
        {
            ModelState.AddModelError(nameof(filter.ToDate), "Đến ngày phải lớn hơn hoặc bằng từ ngày.");
            return View(new AuditIndexViewModel
            {
                Filter = new AuditActivityFilter(
                    filter.FromDate,
                    filter.ToDate,
                    filter.Operation,
                    filter.Search,
                    1),
                Results = new AuditActivityPage([], 1, 25, 0),
            });
        }

        var activityFilter = new AuditActivityFilter(
            filter.FromDate,
            filter.ToDate,
            filter.Operation,
            filter.Search,
            filter.Page);
        var results = await _auditService.GetActivitiesAsync(activityFilter, cancellationToken);

        return View(new AuditIndexViewModel
        {
            Filter = activityFilter,
            Results = results,
        });
    }
}
