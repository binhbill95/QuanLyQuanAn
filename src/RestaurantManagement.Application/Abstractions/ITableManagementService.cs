using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Tables;

namespace RestaurantManagement.Application.Abstractions;

public interface ITableManagementService
{
    Task<IReadOnlyList<AreaDto>> GetAreasAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AreaOptionDto>> GetAreaOptionsAsync(CancellationToken cancellationToken = default);

    Task<AreaInput?> GetAreaInputAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAreaAsync(AreaInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateAreaAsync(int id, AreaInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteAreaAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiningTableDto>> GetTablesAsync(CancellationToken cancellationToken = default);

    Task<DiningTableInput?> GetTableInputAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateTableAsync(DiningTableInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateTableAsync(int id, DiningTableInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetTableActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteTableAsync(int id, CancellationToken cancellationToken = default);
}
