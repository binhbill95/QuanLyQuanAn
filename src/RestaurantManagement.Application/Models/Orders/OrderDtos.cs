namespace RestaurantManagement.Application.Models.Orders;

public sealed record ActiveTableOrderDto(
    int TableId,
    string TableNumber,
    string? AreaName,
    int MaxSeats,
    string StatusName,
    int? InvoiceId,
    string? InvoiceCode);

public sealed record OrderMenuOptionDto(
    int Id,
    string Code,
    string Name,
    string CategoryName,
    decimal Price,
    string? Unit);

public sealed record OrderLineDto(
    int Id,
    int MenuItemId,
    string ItemCode,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    string? Unit,
    string? Note,
    byte Status,
    DateTime OrderedAt,
    string? CancelReason);

public sealed record OrderTableDto(int Id, string Number, bool IsPrimary);

public sealed record ActiveInvoiceDto(
    int Id,
    string InvoiceCode,
    int CustomerCount,
    DateTime OpenedAt,
    IReadOnlyList<OrderTableDto> Tables,
    decimal Subtotal);

public sealed record InvoiceOrderDto(
    int Id,
    string InvoiceCode,
    int CustomerCount,
    DateTime OpenedAt,
    IReadOnlyList<OrderTableDto> Tables,
    IReadOnlyList<OrderLineDto> Items,
    decimal Subtotal);

public sealed record KitchenQueueItemDto(
    int DetailId,
    int InvoiceId,
    string InvoiceCode,
    string? TableNumber,
    string ItemCode,
    string ItemName,
    int Quantity,
    string? Note,
    byte Status,
    DateTime OrderedAt,
    int? WaitingMinutes);

public sealed record OpenInvoiceInput(int TableId, int CustomerCount);

public sealed record AddOrderItemInput(int MenuItemId, int Quantity, string? Note);
