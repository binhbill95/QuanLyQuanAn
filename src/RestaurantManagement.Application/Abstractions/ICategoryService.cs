using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Menu;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>Quản lý nhóm món (Category).</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryOptionDto>> GetOptionsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<CategoryInput?> GetInputAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(
        CategoryInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateAsync(
        int id,
        CategoryInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
