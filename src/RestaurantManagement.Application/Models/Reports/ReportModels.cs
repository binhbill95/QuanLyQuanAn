namespace RestaurantManagement.Application.Models.Reports;

public sealed record ReportFilter(
    int Year,
    int? Month,
    int? Quarter,
    int? Week,
    int? ShiftId,
    int? EmployeeId,
    int? TableId,
    int? MenuItemId);

public sealed record RevenueReportRow(int Year, int Quarter, int Month, int InvoiceCount, int CustomerCount, decimal Revenue);

public sealed record CustomerByShiftReportRow(
    int Year,
    int Quarter,
    int Month,
    int ShiftId,
    string ShiftName,
    int InvoiceCount,
    int CustomerCount);

public sealed record CustomerByEmployeeReportRow(
    int Year,
    int Week,
    DateOnly? WeekStartDate,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    int InvoiceCount,
    int CustomerCount);

public sealed record CustomerByTableReportRow(
    int Year,
    int Quarter,
    int Month,
    int TableId,
    string TableNumber,
    int InvoiceCount,
    int CustomerCount);

public sealed record ItemSalesReportRow(
    int Year,
    int Quarter,
    int Month,
    int MenuItemId,
    string ItemCode,
    string ItemName,
    string CategoryName,
    int Quantity,
    decimal Revenue);

public sealed record PayrollReportRow(
    int Year,
    int Month,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    int ApprovedShiftCount,
    decimal ShiftRate,
    decimal Salary);

public sealed record ReportsDashboardDto(
    IReadOnlyList<RevenueReportRow> Revenue,
    IReadOnlyList<CustomerByShiftReportRow> CustomersByShift,
    IReadOnlyList<CustomerByEmployeeReportRow> CustomersByEmployee,
    IReadOnlyList<CustomerByTableReportRow> CustomersByTable,
    IReadOnlyList<ItemSalesReportRow> ItemSales,
    IReadOnlyList<PayrollReportRow> Payroll);
