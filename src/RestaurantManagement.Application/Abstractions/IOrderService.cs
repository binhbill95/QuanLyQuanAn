using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Orders;

namespace RestaurantManagement.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<ActiveTableOrderDto>> GetTablesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderMenuOptionDto>> GetMenuAsync(CancellationToken cancellationToken = default);

    Task<InvoiceOrderDto?> GetInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActiveInvoiceDto>> GetActiveInvoicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KitchenQueueItemDto>> GetKitchenQueueAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<int>> OpenInvoiceAsync(
        OpenInvoiceInput input,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> AddItemAsync(
        int invoiceId,
        AddOrderItemInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> CancelItemAsync(
        int invoiceId,
        int detailId,
        string reason,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ReplaceItemAsync(
        int invoiceId,
        int detailId,
        AddOrderItemInput replacement,
        string reason,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetKitchenStatusAsync(
        int detailId,
        byte status,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> MoveTableAsync(
        int invoiceId,
        int fromTableId,
        int toTableId,
        int employeeId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> MergeInvoicesAsync(
        int targetInvoiceId,
        int sourceInvoiceId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
