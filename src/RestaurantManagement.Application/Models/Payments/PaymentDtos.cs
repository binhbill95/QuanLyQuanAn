using RestaurantManagement.Application.Models.Orders;

namespace RestaurantManagement.Application.Models.Payments;

public sealed record PaymentCheckoutDto(
    int InvoiceId,
    string InvoiceCode,
    int CustomerCount,
    DateTime OpenedAt,
    IReadOnlyList<OrderTableDto> Tables,
    IReadOnlyList<OrderLineDto> Items,
    decimal Subtotal,
    int UnfinishedItemCount);

public sealed record PaymentReceiptDto(
    int InvoiceId,
    string InvoiceCode,
    int CustomerCount,
    DateTime OpenedAt,
    IReadOnlyList<OrderTableDto> Tables,
    IReadOnlyList<OrderLineDto> Items,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal AmountPaid,
    byte Method,
    DateTime PaidAt,
    string PaidByEmployeeName,
    string? Note);

public sealed record PayInvoiceInput(
    int InvoiceId,
    byte Method,
    decimal DiscountAmount,
    bool ConfirmUnfinishedItems,
    string? Note);
