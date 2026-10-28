using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Orders;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "OrderManagement")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(
        int? tableId,
        int? invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = invoiceId.HasValue
            ? await _orderService.GetInvoiceAsync(invoiceId.Value, cancellationToken)
            : null;
        if (invoiceId.HasValue && invoice is null)
        {
            return NotFound();
        }

        var canManageTables = User.IsInRole("ADMIN") ||
                              User.IsInRole("OWNER") ||
                              User.IsInRole("MANAGER");
        var tables = await _orderService.GetTablesAsync(cancellationToken);

        return View(new OrderIndexViewModel
        {
            Tables = tables,
            MenuItems = await _orderService.GetMenuAsync(cancellationToken),
            ActiveInvoices = await _orderService.GetActiveInvoicesAsync(cancellationToken),
            Invoice = invoice,
            SelectedTableId = tableId,
            CanManageTables = canManageTables,
            OpenInvoice = new OpenInvoiceFormViewModel { TableId = tableId ?? 0 },
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(
        OpenInvoiceFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên nên không thể mở bàn.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn bàn và nhập số lượng khách hợp lệ.";
            return RedirectToAction(nameof(Index), new { tableId = model.TableId > 0 ? (int?)model.TableId : null });
        }

        var result = await _orderService.OpenInvoiceAsync(
            new OpenInvoiceInput(model.TableId, model.CustomerCount),
            employeeId.Value,
            cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Index), new { tableId = model.TableId });
        }

        TempData["SuccessMessage"] = "Đã mở bàn và tạo hóa đơn.";
        return RedirectToAction(nameof(Index), new { invoiceId = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(
        AddOrderItemFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn món và số lượng hợp lệ.";
            return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
        }

        var result = await _orderService.AddItemAsync(
            model.InvoiceId,
            new AddOrderItemInput(model.MenuItemId, model.Quantity, model.Note),
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã thêm món vào hóa đơn." : result.Error;
        return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelItem(
        CancelOrderItemFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy món.";
            return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
        }

        var result = await _orderService.CancelItemAsync(
            model.InvoiceId,
            model.DetailId,
            model.Reason,
            employeeId.Value,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã hủy món đang chờ." : result.Error;
        return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplaceItem(
        ReplaceOrderItemFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn món thay thế, số lượng và lý do đổi hợp lệ.";
            return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
        }

        var result = await _orderService.ReplaceItemAsync(
            model.InvoiceId,
            model.DetailId,
            new AddOrderItemInput(model.MenuItemId, model.Quantity, model.Note),
            model.Reason,
            employeeId.Value,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã đổi món; món mới đã được gửi vào hàng đợi bếp." : result.Error;
        return RedirectToAction(nameof(Index), new { invoiceId = model.InvoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveTable(
        int invoiceId,
        int fromTableId,
        int toTableId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { invoiceId });
        }

        var result = await _orderService.MoveTableAsync(
            invoiceId,
            fromTableId,
            toTableId,
            employeeId.Value,
            note,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã chuyển bàn." : result.Error;
        return RedirectToAction(nameof(Index), new { invoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Merge(
        int targetInvoiceId,
        int sourceInvoiceId,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
        {
            TempData["ErrorMessage"] = "Tài khoản chưa gắn hồ sơ nhân viên.";
            return RedirectToAction(nameof(Index), new { invoiceId = targetInvoiceId });
        }

        var result = await _orderService.MergeInvoicesAsync(
            targetInvoiceId,
            sourceInvoiceId,
            employeeId.Value,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã gộp hóa đơn và bàn." : result.Error;
        return RedirectToAction(nameof(Index), new { invoiceId = targetInvoiceId });
    }

    private int? GetEmployeeId()
        => int.TryParse(User.FindFirst("employee_id")?.Value, out var employeeId)
            ? employeeId
            : null;
}
