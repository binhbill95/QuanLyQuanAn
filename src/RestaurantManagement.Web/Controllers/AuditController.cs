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
    private readonly IBranchService _branchService;

    public AuditController(IAuditService auditService, IBranchService branchService)
    {
        _auditService = auditService;
        _branchService = branchService;
    }

    public async Task<IActionResult> Index(
        AuditFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = User.IsInRole("ADMIN");
        var branchId = isAdmin ? filter.BranchId : null;

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
                    1,
                    branchId),
                Results = new AuditActivityPage([], 1, 25, 0),
                Branches = isAdmin
                    ? await _branchService.GetOptionsAsync(cancellationToken)
                    : [],
                ShowBranchFilter = isAdmin,
            });
        }

        var activityFilter = new AuditActivityFilter(
            filter.FromDate,
            filter.ToDate,
            filter.Operation,
            filter.Search,
            filter.Page,
            branchId);
        var results = await _auditService.GetActivitiesAsync(activityFilter, cancellationToken);

        return View(new AuditIndexViewModel
        {
            Filter = activityFilter,
            Results = results,
            Branches = isAdmin
                ? await _branchService.GetOptionsAsync(cancellationToken)
                : [],
            ShowBranchFilter = isAdmin,
        });
    }
}
