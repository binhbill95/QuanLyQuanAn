using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Payments;

namespace RestaurantManagement.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentCheckoutDto?> GetCheckoutAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);

    Task<PaymentReceiptDto?> GetReceiptAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<int>> PayAsync(
        PayInvoiceInput input,
        int employeeId,
        CancellationToken cancellationToken = default);
}
