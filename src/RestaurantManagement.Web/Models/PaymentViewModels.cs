using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Orders;
using RestaurantManagement.Application.Models.Payments;

namespace RestaurantManagement.Web.Models;

public sealed class PaymentIndexViewModel
{
    public IReadOnlyList<ActiveInvoiceDto> Invoices { get; init; } = [];
}

public sealed class PaymentCheckoutViewModel
{
    public required PaymentCheckoutDto Invoice { get; init; }

    public required PaymentFormViewModel Form { get; init; }

    public bool CanApplyDiscount { get; init; }
}

public sealed class PaymentFormViewModel
{
    [Range(1, int.MaxValue)]
    public int InvoiceId { get; set; }

    [Range(0, 1, ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    public byte Method { get; set; }

    [Range(typeof(decimal), "0", "999999999999999999.99",
        ErrorMessage = "Mức giảm giá không hợp lệ.")]
    public decimal DiscountAmount { get; set; }

    public bool ConfirmUnfinishedItems { get; set; }

    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }
}
