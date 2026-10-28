using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Menu;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>Quản lý món ăn/nước uống (MenuItem).</summary>
public interface IMenuService
{
    Task<IReadOnlyList<MenuItemListItemDto>> GetAsync(
        string? keyword,
        int? categoryId,
        bool? isAvailable,
        bool includeInactiveCategories = false,
        bool showDeleted = false,
        CancellationToken cancellationToken = default);

    Task<MenuItemInput?> GetInputAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<string> SuggestCodeAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(
        MenuItemInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateAsync(
        int id,
        MenuItemInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetAvailableAsync(
        int id,
        bool isAvailable,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> RestoreAsync(
        int id,
        int categoryId,
        CancellationToken cancellationToken = default);
}
