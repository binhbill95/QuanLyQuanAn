using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Orders;
using RestaurantManagement.Infrastructure.Payments;
using RestaurantManagement.Infrastructure.Persistence;
using RestaurantManagement.Infrastructure.Shifts;
using RestaurantManagement.Infrastructure.Timekeeping;
using Xunit;

namespace RestaurantManagement.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESTAURANT_TEST_CONNECTION_STRING")))
        {
            Skip = "Set RESTAURANT_TEST_CONNECTION_STRING to an isolated RestaurantManagement_AutomationTest database.";
        }
    }
}

public sealed class BusinessWorkflowTests
{
    [SqlServerFact]
    public async Task MoveTable_rejects_destination_below_source_guest_count()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (invoice, sourceTable) = await scenario.CreateInvoiceAsync(4, 4);
        var targetTable = await scenario.CreateTableAsync(2);

        var service = scenario.CreateOrderService();
        var result = await service.MoveTableAsync(invoice.Id, sourceTable.Id, targetTable.Id, scenario.Employee.Id, null);

        Assert.False(result.Succeeded);
        Assert.Contains("chỉ phục vụ tối đa", result.Error, StringComparison.OrdinalIgnoreCase);
        var sourceLink = await scenario.Db.InvoiceTables.SingleAsync(
            t => t.InvoiceId == invoice.Id && t.TableId == sourceTable.Id);
        Assert.Null(sourceLink.ReleasedAt);
        Assert.False(await scenario.Db.InvoiceTables.AnyAsync(t => t.InvoiceId == invoice.Id && t.TableId == targetTable.Id));
    }

    [SqlServerFact]
    public async Task MoveTable_preserves_guest_count_when_destination_has_capacity()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (invoice, sourceTable) = await scenario.CreateInvoiceAsync(4, 4);
        var targetTable = await scenario.CreateTableAsync(4);

        var result = await scenario.CreateOrderService().MoveTableAsync(
            invoice.Id,
            sourceTable.Id,
            targetTable.Id,
            scenario.Employee.Id,
            null);

        Assert.True(result.Succeeded, result.Error);
        var links = await scenario.Db.InvoiceTables
            .Where(t => t.InvoiceId == invoice.Id)
            .OrderBy(t => t.Id)
            .ToListAsync();
        Assert.Equal(4, links[0].CustomerCount);
        Assert.NotNull(links[0].ReleasedAt);
        Assert.Equal(4, links[1].CustomerCount);
        Assert.Null(links[1].ReleasedAt);
    }

    [SqlServerFact]
    public async Task MergeInvoices_preserves_each_tables_guest_count_in_report()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (targetInvoice, targetTable) = await scenario.CreateInvoiceAsync(4, 4);
        var (sourceInvoice, sourceTable) = await scenario.CreateInvoiceAsync(3, 3);

        var result = await scenario.CreateOrderService().MergeInvoicesAsync(
            targetInvoice.Id,
            sourceInvoice.Id,
            scenario.Employee.Id);
        Assert.True(result.Succeeded, result.Error);

        targetInvoice.Status = 1;
        targetInvoice.ClosedAt = DateTime.UtcNow;
        await scenario.Db.SaveChangesAsync();

        var rows = await scenario.Db.VwCustomerByTables
            .Where(row => row.TableId == targetTable.Id || row.TableId == sourceTable.Id)
            .ToListAsync();

        Assert.Equal(4, Assert.Single(rows, row => row.TableId == targetTable.Id).CustomerCount);
        Assert.Equal(3, Assert.Single(rows, row => row.TableId == sourceTable.Id).CustomerCount);
        Assert.Equal(7, targetInvoice.CustomerCount);
    }

    [SqlServerFact]
    public async Task MoveTable_rejects_a_destination_in_another_branch()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (invoice, sourceTable) = await scenario.CreateInvoiceAsync(2, 4);
        var otherBranchId = await scenario.Db.Branches
            .Where(branch => branch.Code == "CN002")
            .Select(branch => branch.Id)
            .SingleAsync();
        var otherBranchTable = await scenario.CreateTableAsync(4, otherBranchId);

        var result = await scenario.CreateOrderService().MoveTableAsync(
            invoice.Id,
            sourceTable.Id,
            otherBranchTable.Id,
            scenario.Employee.Id,
            null);

        Assert.False(result.Succeeded);
        Assert.Contains("chi nhánh", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task MergeInvoices_rejects_invoices_from_different_branches()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (targetInvoice, _) = await scenario.CreateInvoiceAsync(2, 4);
        var otherBranchId = await scenario.Db.Branches
            .Where(branch => branch.Code == "CN002")
            .Select(branch => branch.Id)
            .SingleAsync();
        var (sourceInvoice, _) = await scenario.CreateInvoiceAsync(2, 4, otherBranchId);

        var result = await scenario.CreateOrderService().MergeInvoicesAsync(
            targetInvoice.Id,
            sourceInvoice.Id,
            scenario.Employee.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("chi nhánh", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task Branch_scoped_queries_hide_other_branch_tables_and_invoices()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (otherBranchInvoice, otherBranchTable) = await scenario.CreateInvoiceAsync(
            2,
            4,
            await scenario.Db.Branches
                .Where(branch => branch.Code == "CN002")
                .Select(branch => branch.Id)
                .SingleAsync());
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(scenario.Db.Database.GetConnectionString())
            .Options;
        await using var branchDb = new RestaurantDbContext(
            options,
            new TestScenario.TestCurrentUser(scenario.BranchId));

        Assert.False(await branchDb.DiningTables.AnyAsync(table => table.Id == otherBranchTable.Id));
        Assert.False(await branchDb.Invoices.AnyAsync(invoice => invoice.Id == otherBranchInvoice.Id));
    }

    [SqlServerFact]
    public async Task Customer_by_table_counts_an_invoice_once_after_returning_to_same_table()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (invoice, firstTable) = await scenario.CreateInvoiceAsync(4, 4);
        var secondTable = await scenario.CreateTableAsync(4);
        var service = scenario.CreateOrderService();

        var firstMove = await service.MoveTableAsync(
            invoice.Id,
            firstTable.Id,
            secondTable.Id,
            scenario.Employee.Id,
            null);
        Assert.True(firstMove.Succeeded, firstMove.Error);
        var returnMove = await service.MoveTableAsync(
            invoice.Id,
            secondTable.Id,
            firstTable.Id,
            scenario.Employee.Id,
            null);
        Assert.True(returnMove.Succeeded, returnMove.Error);

        invoice.Status = 1;
        invoice.ClosedAt = DateTime.UtcNow;
        await scenario.Db.SaveChangesAsync();

        var firstTableReport = await scenario.Db.VwCustomerByTables.SingleAsync(
            row => row.TableId == firstTable.Id);
        Assert.Equal(1, firstTableReport.InvoiceCount);
        Assert.Equal(4, firstTableReport.CustomerCount);
    }

    [SqlServerFact]
    public async Task Receipt_only_lists_tables_still_assigned_to_invoice()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var (invoice, oldTable) = await scenario.CreateInvoiceAsync(2, 2);
        var currentTable = await scenario.CreateTableAsync(4);
        var oldLink = await scenario.Db.InvoiceTables.SingleAsync(
            table => table.InvoiceId == invoice.Id && table.TableId == oldTable.Id);
        oldLink.ReleasedAt = oldLink.AssignedAt.AddSeconds(1);
        var currentAssignedAt = oldLink.ReleasedAt.Value.AddSeconds(1);
        var paidAt = currentAssignedAt.AddMinutes(1);
        scenario.Db.InvoiceTables.Add(new InvoiceTable
        {
            InvoiceId = invoice.Id,
            TableId = currentTable.Id,
            CustomerCount = 2,
            AssignedAt = currentAssignedAt,
            ReleasedAt = paidAt,
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
        });
        invoice.Status = 1;
        invoice.ClosedAt = paidAt;
        scenario.Db.Payments.Add(new Payment
        {
            InvoiceId = invoice.Id,
            Method = 0,
            Amount = 0,
            PaidAt = paidAt,
            PaidByEmployeeId = scenario.Employee.Id,
            CreatedAt = DateTime.UtcNow,
        });
        await scenario.Db.SaveChangesAsync();

        var receipt = await scenario.CreatePaymentService().GetReceiptAsync(invoice.Id);

        Assert.NotNull(receipt);
        Assert.Equal(new[] { currentTable.TableNumber }, receipt.Tables.Select(table => table.Number));
    }

    [SqlServerFact]
    public async Task Approved_attendance_without_checkout_is_counted_for_payroll()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var shift = new Shift
        {
            Name = $"AUTOTEST-{Guid.NewGuid():N}"[..20],
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        scenario.Db.Shifts.Add(shift);
        await scenario.Db.SaveChangesAsync();
        scenario.TrackShift(shift.Id);
        var schedule = new Schedule
        {
            EmployeeId = scenario.Employee.Id,
            ShiftId = shift.Id,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = 0,
            CreatedAt = DateTime.UtcNow,
        };
        scenario.Db.Schedules.Add(schedule);
        await scenario.Db.SaveChangesAsync();
        scenario.TrackSchedule(schedule.Id);
        var attendance = new Attendance
        {
            ScheduleId = schedule.Id,
            CheckInAt = DateTime.UtcNow.AddHours(-1),
            Status = 0,
            CreatedAt = DateTime.UtcNow,
        };
        scenario.Db.Attendances.Add(attendance);
        await scenario.Db.SaveChangesAsync();
        scenario.TrackAttendance(attendance.Id);

        var result = await scenario.CreateAttendanceService().ApproveAsync(attendance.Id, null);
        Assert.True(result.Succeeded, result.Error);

        var payroll = await scenario.Db.VwPayrolls.SingleAsync(row =>
            row.EmployeeId == scenario.Employee.Id &&
            row.Year == schedule.WorkDate.Year &&
            row.Month == schedule.WorkDate.Month);

        Assert.Null(attendance.CheckOutAt);
        Assert.Equal(1, payroll.ApprovedShiftCount);
    }

    [SqlServerFact]
    public async Task ShiftService_rejects_overlaps_but_allows_adjacent_and_inactive_shifts()
    {
        await using var scenario = await TestScenario.CreateAsync();
        await scenario.CreateShiftAsync("22:00", "23:00", true);
        var service = scenario.CreateShiftService();

        var overlapping = await service.CreateAsync(new(
            $"Overlap-{Guid.NewGuid():N}"[..20],
            new TimeOnly(22, 30),
            new TimeOnly(23, 30),
            true));
        Assert.False(overlapping.Succeeded);

        var adjacent = await service.CreateAsync(new(
            $"Adjacent-{Guid.NewGuid():N}"[..20],
            new TimeOnly(23, 0),
            new TimeOnly(23, 30),
            true));
        Assert.True(adjacent.Succeeded, adjacent.Error);
        scenario.TrackShift(adjacent.Value);

        var inactiveOverlap = await service.CreateAsync(new(
            $"Inactive-{Guid.NewGuid():N}"[..20],
            new TimeOnly(22, 30),
            new TimeOnly(23, 30),
            false));
        Assert.True(inactiveOverlap.Succeeded, inactiveOverlap.Error);
        scenario.TrackShift(inactiveOverlap.Value);

        var activation = await service.SetActiveAsync(inactiveOverlap.Value, true);
        Assert.False(activation.Succeeded);
    }

    [SqlServerFact]
    public async Task Customer_by_employee_uses_iso_year_and_monday_week_start()
    {
        await using var scenario = await TestScenario.CreateAsync();
        var closedAt = new DateTime(2021, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice
        {
            InvoiceCode = scenario.NewInvoiceCode(),
            CustomerCount = 2,
            OpenedByEmployeeId = scenario.Employee.Id,
            BranchId = scenario.BranchId,
            OpenedAt = closedAt.AddHours(-1),
            ClosedAt = closedAt,
            Status = 1,
            CreatedAt = closedAt.AddHours(-1),
        };
        scenario.Db.Invoices.Add(invoice);
        await scenario.Db.SaveChangesAsync();
        scenario.TrackInvoice(invoice.Id);

        var report = await scenario.Db.VwCustomerByEmployees.SingleAsync(row =>
            row.EmployeeId == scenario.Employee.Id &&
            row.Year == 2020 &&
            row.Week == 53);

        Assert.Equal(new DateOnly(2020, 12, 28), report.WeekStartDate);
        Assert.Equal(2, report.CustomerCount);
    }
}

internal sealed class TestScenario : IAsyncDisposable
{
    private readonly TestCurrentUser _currentUser;
    private readonly List<int> _invoiceIds = [];
    private readonly List<int> _tableIds = [];
    private readonly List<int> _scheduleIds = [];
    private readonly List<int> _attendanceIds = [];
    private readonly List<int> _shiftIds = [];
    private int? _employeeId;
    private int? _positionId;
    private int _branchId;

    private TestScenario(RestaurantDbContext db, TestCurrentUser currentUser)
    {
        Db = db;
        _currentUser = currentUser;
    }

    public RestaurantDbContext Db { get; }

    public Employee Employee { get; private set; } = null!;
    public int BranchId => _branchId;

    public static async Task<TestScenario> CreateAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("RESTAURANT_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("RESTAURANT_TEST_CONNECTION_STRING is required.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "RestaurantManagement_AutomationTest", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Integration tests only run against RestaurantManagement_AutomationTest.");
        }

        var currentUser = new TestCurrentUser();
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var db = new RestaurantDbContext(options, currentUser);
        var scenario = new TestScenario(db, currentUser);
        scenario._branchId = await db.Branches
            .Where(branch => branch.Code == "CN001")
            .Select(branch => branch.Id)
            .SingleAsync();

        var position = new Position
        {
            Code = $"T{Guid.NewGuid():N}"[..20],
            Name = $"Test {Guid.NewGuid():N}"[..30],
            DefaultShiftRate = 100000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.Positions.Add(position);
        await db.SaveChangesAsync();
        scenario._positionId = position.Id;

        var employee = new Employee
        {
            EmployeeCode = $"T{Guid.NewGuid():N}"[..20],
            FullName = "Automation Test",
            IdentityNumber = Guid.NewGuid().ToString("N")[..20],
            PositionId = position.Id,
            BranchId = scenario._branchId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        scenario.Employee = employee;
        scenario._employeeId = employee.Id;
        return scenario;
    }

    public OrderService CreateOrderService() => new(Db, _currentUser);

    public PaymentService CreatePaymentService() => new(Db, _currentUser);

    public ShiftService CreateShiftService() => new(Db, _currentUser);

    public AttendanceService CreateAttendanceService() => new(Db, _currentUser);

    public string NewInvoiceCode() => $"I{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    public void TrackInvoice(int id) => _invoiceIds.Add(id);

    public void TrackShift(int id) => _shiftIds.Add(id);

    public void TrackSchedule(int id) => _scheduleIds.Add(id);

    public void TrackAttendance(int id) => _attendanceIds.Add(id);

    public async Task<DiningTable> CreateTableAsync(int maxSeats, int? branchId = null)
    {
        var table = new DiningTable
        {
            TableNumber = $"T{Guid.NewGuid():N}"[..10],
            BranchId = branchId ?? _branchId,
            MaxSeats = maxSeats,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        Db.DiningTables.Add(table);
        await Db.SaveChangesAsync();
        _tableIds.Add(table.Id);
        return table;
    }

    public async Task<(Invoice Invoice, DiningTable Table)> CreateInvoiceAsync(
        int customerCount,
        int maxSeats,
        int? branchId = null)
    {
        var invoiceBranchId = branchId ?? _branchId;
        var table = await CreateTableAsync(maxSeats, invoiceBranchId);
        var invoice = new Invoice
        {
            InvoiceCode = NewInvoiceCode(),
            CustomerCount = customerCount,
            OpenedByEmployeeId = Employee.Id,
            BranchId = invoiceBranchId,
            OpenedAt = DateTime.UtcNow,
            Status = 0,
            CreatedAt = DateTime.UtcNow,
        };
        Db.Invoices.Add(invoice);
        await Db.SaveChangesAsync();
        _invoiceIds.Add(invoice.Id);
        Db.InvoiceTables.Add(new InvoiceTable
        {
            InvoiceId = invoice.Id,
            TableId = table.Id,
            CustomerCount = customerCount,
            AssignedAt = DateTime.UtcNow,
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
        });
        await Db.SaveChangesAsync();
        return (invoice, table);
    }

    public async Task<Shift> CreateShiftAsync(string start, string end, bool isActive)
    {
        var shift = new Shift
        {
            Name = $"AUTOTEST-{Guid.NewGuid():N}"[..20],
            StartTime = TimeOnly.Parse(start, CultureInfo.InvariantCulture),
            EndTime = TimeOnly.Parse(end, CultureInfo.InvariantCulture),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
        Db.Shifts.Add(shift);
        await Db.SaveChangesAsync();
        TrackShift(shift.Id);
        return shift;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var invoiceId in _invoiceIds)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.TableTransferLog WHERE InvoiceId = {invoiceId}");
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Payment WHERE InvoiceId = {invoiceId}");
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.InvoiceDetail WHERE InvoiceId = {invoiceId}");
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.InvoiceTable WHERE InvoiceId = {invoiceId}");
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Invoice WHERE Id = {invoiceId}");
            await DeleteAuditRecordsAsync("Invoice", invoiceId);
        }

        foreach (var attendanceId in _attendanceIds)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Attendance WHERE Id = {attendanceId}");
            await DeleteAuditRecordsAsync("Attendance", attendanceId);
        }

        foreach (var scheduleId in _scheduleIds)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Schedule WHERE Id = {scheduleId}");
            await DeleteAuditRecordsAsync("Schedule", scheduleId);
        }

        foreach (var shiftId in _shiftIds)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Shift WHERE Id = {shiftId}");
            await DeleteAuditRecordsAsync("Shift", shiftId);
        }

        foreach (var tableId in _tableIds)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.DiningTable WHERE Id = {tableId}");
        }

        if (_employeeId.HasValue)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Employee WHERE Id = {_employeeId.Value}");
            await DeleteAuditRecordsAsync("Employee", _employeeId.Value);
        }

        if (_positionId.HasValue)
        {
            await Db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Position WHERE Id = {_positionId.Value}");
        }

        await Db.DisposeAsync();
    }

    private Task<int> DeleteAuditRecordsAsync(string tableName, int recordId)
    {
        var recordIdText = recordId.ToString(CultureInfo.InvariantCulture);
        return Db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE dbo.AuditLog WHERE TableName = {tableName} AND RecordId = {recordIdText}");
    }

    internal sealed class TestCurrentUser : ICurrentUserService
    {
        public TestCurrentUser(int? branchId = null) => BranchId = branchId;

        public int? UserId => null;
        public string? UserName => "automation-test";
        public string? RoleCode => "ADMIN";
        public int? EmployeeId => null;
        public int? BranchId { get; }
        public string? BranchName => null;
        public bool IsAuthenticated => true;
    }
}
