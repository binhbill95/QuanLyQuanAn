using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Reports;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Reporting;

public sealed class ReportingService : IReportingService
{
    private readonly RestaurantDbContext _db;

    public ReportingService(RestaurantDbContext db) => _db = db;

    public async Task<ReportsDashboardDto> GetDashboardAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var revenueQuery = _db.VwRevenueByPeriods
            .AsNoTracking()
            .Where(row => row.Year == filter.Year);
        if (filter.Month.HasValue)
        {
            revenueQuery = revenueQuery.Where(row => row.Month == filter.Month);
        }
        if (filter.Quarter.HasValue)
        {
            revenueQuery = revenueQuery.Where(row => row.Quarter == filter.Quarter);
        }

        var shiftQuery = _db.VwCustomerByShifts
            .AsNoTracking()
            .Where(row => row.Year == filter.Year);
        if (filter.Month.HasValue)
        {
            shiftQuery = shiftQuery.Where(row => row.Month == filter.Month);
        }
        if (filter.Quarter.HasValue)
        {
            shiftQuery = shiftQuery.Where(row => row.Quarter == filter.Quarter);
        }
        if (filter.ShiftId.HasValue)
        {
            shiftQuery = shiftQuery.Where(row => row.ShiftId == filter.ShiftId);
        }

        var employeeQuery = _db.VwCustomerByEmployees
            .AsNoTracking()
            .Where(row => row.Year == filter.Year);
        if (filter.Week.HasValue)
        {
            employeeQuery = employeeQuery.Where(row => row.Week == filter.Week);
        }
        if (filter.EmployeeId.HasValue)
        {
            employeeQuery = employeeQuery.Where(row => row.EmployeeId == filter.EmployeeId);
        }

        var tableQuery = _db.VwCustomerByTables
            .AsNoTracking()
            .Where(row => row.Year == filter.Year);
        if (filter.Month.HasValue)
        {
            tableQuery = tableQuery.Where(row => row.Month == filter.Month);
        }
        if (filter.Quarter.HasValue)
        {
            tableQuery = tableQuery.Where(row => row.Quarter == filter.Quarter);
        }
        if (filter.TableId.HasValue)
        {
            tableQuery = tableQuery.Where(row => row.TableId == filter.TableId);
        }

        var itemQuery = _db.VwItemSales
            .AsNoTracking()
            .Where(row => row.Year == filter.Year);
        if (filter.Month.HasValue)
        {
            itemQuery = itemQuery.Where(row => row.Month == filter.Month);
        }
        if (filter.Quarter.HasValue)
        {
            itemQuery = itemQuery.Where(row => row.Quarter == filter.Quarter);
        }
        if (filter.MenuItemId.HasValue)
        {
            itemQuery = itemQuery.Where(row => row.MenuItemId == filter.MenuItemId);
        }

        var payrollQuery = _db.VwPayrolls
            .AsNoTracking()
            .Where(row => row.Year == filter.Year);
        if (filter.Month.HasValue)
        {
            payrollQuery = payrollQuery.Where(row => row.Month == filter.Month);
        }
        if (filter.EmployeeId.HasValue)
        {
            payrollQuery = payrollQuery.Where(row => row.EmployeeId == filter.EmployeeId);
        }

        var revenue = await revenueQuery
            .OrderBy(row => row.Month)
            .Select(row => new RevenueReportRow(
                row.Year!.Value,
                row.Quarter!.Value,
                row.Month!.Value,
                row.InvoiceCount ?? 0,
                row.CustomerCount ?? 0,
                row.Revenue ?? 0m))
            .ToListAsync(cancellationToken);

        var customersByShift = await shiftQuery
            .OrderBy(row => row.Month)
            .ThenBy(row => row.ShiftName)
            .Select(row => new CustomerByShiftReportRow(
                row.Year!.Value,
                row.Quarter!.Value,
                row.Month!.Value,
                row.ShiftId,
                row.ShiftName,
                row.InvoiceCount ?? 0,
                row.CustomerCount ?? 0))
            .ToListAsync(cancellationToken);

        var customersByEmployee = await employeeQuery
            .OrderBy(row => row.Week)
            .ThenBy(row => row.FullName)
            .Select(row => new CustomerByEmployeeReportRow(
                row.Year!.Value,
                row.Week!.Value,
                row.WeekStartDate,
                row.EmployeeId,
                row.EmployeeCode,
                row.FullName,
                row.InvoiceCount ?? 0,
                row.CustomerCount ?? 0))
            .ToListAsync(cancellationToken);

        var customersByTable = await tableQuery
            .OrderBy(row => row.Month)
            .ThenBy(row => row.TableNumber)
            .Select(row => new CustomerByTableReportRow(
                row.Year!.Value,
                row.Quarter!.Value,
                row.Month!.Value,
                row.TableId,
                row.TableNumber,
                row.InvoiceCount ?? 0,
                row.CustomerCount ?? 0))
            .ToListAsync(cancellationToken);

        var itemSales = await itemQuery
            .OrderByDescending(row => row.Quantity)
            .ThenBy(row => row.ItemName)
            .Select(row => new ItemSalesReportRow(
                row.Year!.Value,
                row.Quarter!.Value,
                row.Month!.Value,
                row.MenuItemId,
                row.ItemCode,
                row.ItemName,
                row.CategoryName,
                row.Quantity ?? 0,
                row.Revenue ?? 0m))
            .ToListAsync(cancellationToken);

        var payroll = await payrollQuery
            .OrderBy(row => row.FullName)
            .Select(row => new PayrollReportRow(
                row.Year!.Value,
                row.Month!.Value,
                row.EmployeeId,
                row.EmployeeCode,
                row.FullName,
                row.PositionName,
                row.ApprovedShiftCount ?? 0,
                row.ShiftRate,
                row.Salary ?? 0m))
            .ToListAsync(cancellationToken);

        return new ReportsDashboardDto(
            revenue,
            customersByShift,
            customersByEmployee,
            customersByTable,
            itemSales,
            payroll);
    }
}
