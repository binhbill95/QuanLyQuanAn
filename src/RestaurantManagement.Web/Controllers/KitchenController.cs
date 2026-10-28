using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Kitchen")]
public class KitchenController : Controller
{
    private readonly IOrderService _orderService;

    public KitchenController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Queue(CancellationToken cancellationToken = default)
        => View(await _orderService.GetKitchenQueueAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(
        int detailId,
        byte status,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.SetKitchenStatusAsync(detailId, status, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? (status == 1 ? "Đã nhận chế biến món." : "Đã hoàn thành món.")
                : result.Error;
        return RedirectToAction(nameof(Queue));
    }
}
