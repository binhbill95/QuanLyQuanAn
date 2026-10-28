using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Reports;

namespace RestaurantManagement.Web.Models;

public sealed class ReportsFilterViewModel
{
    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.Today.Year;

    [Range(1, 12)]
    public int? Month { get; set; }

    [Range(1, 4)]
    public int? Quarter { get; set; }

    [Range(1, 53)]
    public int? Week { get; set; }

    public int? ShiftId { get; set; }

    public int? EmployeeId { get; set; }

    public int? TableId { get; set; }

    public int? MenuItemId { get; set; }

    public ReportFilter ToFilter() => new(
        Year,
        Month,
        Quarter,
        Week,
        ShiftId,
        EmployeeId,
        TableId,
        MenuItemId);
}

public sealed class ReportsIndexViewModel
{
    public required ReportsFilterViewModel Filter { get; init; }

    public required ReportsDashboardDto Dashboard { get; init; }

    public IReadOnlyList<ReportOptionViewModel> Shifts { get; init; } = [];

    public IReadOnlyList<ReportOptionViewModel> Employees { get; init; } = [];

    public IReadOnlyList<ReportOptionViewModel> Tables { get; init; } = [];

    public IReadOnlyList<ReportOptionViewModel> MenuItems { get; init; } = [];
}

public sealed record ReportOptionViewModel(int Id, string Name);
