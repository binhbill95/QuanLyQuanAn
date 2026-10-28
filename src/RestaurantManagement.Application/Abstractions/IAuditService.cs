using RestaurantManagement.Application.Models.Audit;

namespace RestaurantManagement.Application.Abstractions;

public interface IAuditService
{
    Task<AuditActivityPage> GetActivitiesAsync(
        AuditActivityFilter filter,
        CancellationToken cancellationToken = default);
}
