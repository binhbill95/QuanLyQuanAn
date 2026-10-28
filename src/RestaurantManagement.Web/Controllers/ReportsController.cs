using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Infrastructure.Persistence;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public class ReportsController : Controller
{
    private readonly IReportingService _reportingService;
    private readonly RestaurantDbContext _db;

    public ReportsController(IReportingService reportingService, RestaurantDbContext db)
    {
        _reportingService = reportingService;
        _db = db;
    }

    public async Task<IActionResult> Index(
        ReportsFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ModelState.Clear();
            filter = new ReportsFilterViewModel();
        }

        var dashboard = await _reportingService.GetDashboardAsync(filter.ToFilter(), cancellationToken);
        var options = await GetOptionsAsync(cancellationToken);
        return View(new ReportsIndexViewModel
        {
            Filter = filter,
            Dashboard = dashboard,
            Shifts = options.Shifts,
            Employees = options.Employees,
            Tables = options.Tables,
            MenuItems = options.MenuItems,
        });
    }

    public async Task<IActionResult> Export(
        ReportsFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Bộ lọc báo cáo không hợp lệ.");
        }

        var data = await _reportingService.GetDashboardAsync(filter.ToFilter(), cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("Bao cao doanh thu");
        csv.AppendLine("Nam,Quy,Thang,So hoa don,So khach,Doanh thu");
        foreach (var row in data.Revenue)
        {
            csv.AppendLine($"{row.Year},{row.Quarter},{row.Month},{row.InvoiceCount},{row.CustomerCount},{Number(row.Revenue)}");
        }

        csv.AppendLine();
        csv.AppendLine("Khach theo ca");
        csv.AppendLine("Nam,Quy,Thang,Ca,So hoa don,So khach");
        foreach (var row in data.CustomersByShift)
        {
            csv.AppendLine($"{row.Year},{row.Quarter},{row.Month},{CsvText(row.ShiftName)},{row.InvoiceCount},{row.CustomerCount}");
        }

        csv.AppendLine();
        csv.AppendLine("Khach theo nhan vien theo tuan");
        csv.AppendLine("Nam,Tuan,Ma nhan vien,Nhan vien,So hoa don,So khach");
        foreach (var row in data.CustomersByEmployee)
        {
            csv.AppendLine($"{row.Year},{row.Week},{CsvText(row.EmployeeCode)},{CsvText(row.FullName)},{row.InvoiceCount},{row.CustomerCount}");
        }

        csv.AppendLine();
        csv.AppendLine("Khach theo ban");
        csv.AppendLine("Nam,Quy,Thang,Ban,So hoa don,So khach");
        foreach (var row in data.CustomersByTable)
        {
            csv.AppendLine($"{row.Year},{row.Quarter},{row.Month},{CsvText(row.TableNumber)},{row.InvoiceCount},{row.CustomerCount}");
        }

        csv.AppendLine();
        csv.AppendLine("Mon da ban");
        csv.AppendLine("Nam,Quy,Thang,Ma mon,Ten mon,Nhom mon,So luong,Doanh thu");
        foreach (var row in data.ItemSales)
        {
            csv.AppendLine($"{row.Year},{row.Quarter},{row.Month},{CsvText(row.ItemCode)},{CsvText(row.ItemName)},{CsvText(row.CategoryName)},{row.Quantity},{Number(row.Revenue)}");
        }

        csv.AppendLine();
        csv.AppendLine("Luong theo ca da duyet");
        csv.AppendLine("Nam,Thang,Ma nhan vien,Nhan vien,Vi tri,So ca,Don gia ca,Luong");
        foreach (var row in data.Payroll)
        {
            csv.AppendLine($"{row.Year},{row.Month},{CsvText(row.EmployeeCode)},{CsvText(row.FullName)},{CsvText(row.PositionName)},{row.ApprovedShiftCount},{Number(row.ShiftRate)},{Number(row.Salary)}");
        }

        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(content, "text/csv; charset=utf-8", $"bao-cao-{filter.Year}.csv");
    }

    private async Task<(IReadOnlyList<ReportOptionViewModel> Shifts,
        IReadOnlyList<ReportOptionViewModel> Employees,
        IReadOnlyList<ReportOptionViewModel> Tables,
        IReadOnlyList<ReportOptionViewModel> MenuItems)> GetOptionsAsync(
        CancellationToken cancellationToken)
    {
        var shifts = await _db.Shifts.AsNoTracking()
            .Where(shift => !shift.IsDeleted)
            .OrderBy(shift => shift.StartTime)
            .Select(shift => new ReportOptionViewModel(shift.Id, shift.Name))
            .ToListAsync(cancellationToken);
        var employees = await _db.Employees.AsNoTracking()
            .Where(employee => !employee.IsDeleted)
            .OrderBy(employee => employee.FullName)
            .Select(employee => new ReportOptionViewModel(
                employee.Id,
                employee.EmployeeCode + " · " + employee.FullName))
            .ToListAsync(cancellationToken);
        var tables = await _db.DiningTables.AsNoTracking()
            .Where(table => !table.IsDeleted)
            .OrderBy(table => table.TableNumber)
            .Select(table => new ReportOptionViewModel(table.Id, table.TableNumber))
            .ToListAsync(cancellationToken);
        var menuItems = await _db.MenuItems.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.Name)
            .Select(item => new ReportOptionViewModel(item.Id, item.Code + " · " + item.Name))
            .ToListAsync(cancellationToken);

        return (shifts, employees, tables, menuItems);
    }

    private static string CsvText(string value)
    {
        var safeValue = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? $"'{value}"
            : value;
        return $"\"{safeValue.Replace("\"", "\"\"")}\"";
    }

    private static string Number(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);
}
