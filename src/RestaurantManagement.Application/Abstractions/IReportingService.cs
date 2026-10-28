using RestaurantManagement.Application.Models.Reports;

namespace RestaurantManagement.Application.Abstractions;

public interface IReportingService
{
    Task<ReportsDashboardDto> GetDashboardAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default);
}
