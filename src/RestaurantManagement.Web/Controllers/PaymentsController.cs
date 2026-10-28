using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Payments;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "PaymentManagement")]
public class PaymentsController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public PaymentsController(IOrderService orderService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        return View(new PaymentIndexViewModel
        {
            Invoices = await _orderService.GetActiveInvoicesAsync(cancellationToken),
        });
    }

    public async Task<IActionResult> Checkout(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _paymentService.GetCheckoutAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        return View(new PaymentCheckoutViewModel
        {
            Invoice = invoice,
            Form = new PaymentFormViewModel { InvoiceId = invoice.InvoiceId },
            CanApplyDiscount = User.IsInRole("MANAGER"),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(
        PaymentFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin thanh toán.";
            return RedirectToAction(nameof(Checkout), new { invoiceId = model.InvoiceId });
        }

        if (!int.TryParse(User.FindFirst("employee_id")?.Value, out var employeeId))
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên nên không thể ghi nhận thanh toán.";
            return RedirectToAction(nameof(Checkout), new { invoiceId = model.InvoiceId });
        }

        var result = await _paymentService.PayAsync(
            new PayInvoiceInput(
                model.InvoiceId,
                model.Method,
                model.DiscountAmount,
                model.ConfirmUnfinishedItems,
                model.Note),
            employeeId,
            cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Checkout), new { invoiceId = model.InvoiceId });
        }

        TempData["SuccessMessage"] = "Thanh toán thành công. Hóa đơn đã được đóng.";
        return RedirectToAction(nameof(Receipt), new { invoiceId = result.Value });
    }

    public async Task<IActionResult> Receipt(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _paymentService.GetReceiptAsync(invoiceId, cancellationToken);
        return receipt is null ? NotFound() : View(receipt);
    }
}
