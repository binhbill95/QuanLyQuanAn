using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Tables;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize]
public class TablesController : Controller
{
    private readonly ITableManagementService _tableService;

    public TablesController(ITableManagementService tableService)
    {
        _tableService = tableService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        return View(new DiningTablesIndexViewModel
        {
            Tables = await _tableService.GetTablesAsync(cancellationToken),
            CanManage = CanManage(),
        });
    }

    [Authorize(Policy = "Management")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        return View(new DiningTableFormViewModel
        {
            Areas = await _tableService.GetAreaOptionsAsync(cancellationToken),
        });
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DiningTableFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await ReloadAreasAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _tableService.CreateTableAsync(
            new DiningTableInput(model.TableNumber, model.AreaId, model.MaxSeats, model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm bàn.");
            await ReloadAreasAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã thêm bàn '{model.TableNumber.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "Management")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var input = await _tableService.GetTableInputAsync(id, cancellationToken);
        if (input is null)
        {
            return NotFound();
        }

        return View(new DiningTableFormViewModel
        {
            Id = id,
            TableNumber = input.TableNumber,
            AreaId = input.AreaId,
            MaxSeats = input.MaxSeats,
            IsActive = input.IsActive,
            Areas = await _tableService.GetAreaOptionsAsync(cancellationToken),
        });
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        DiningTableFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await ReloadAreasAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _tableService.UpdateTableAsync(
            id,
            new DiningTableInput(model.TableNumber, model.AreaId, model.MaxSeats, model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật bàn.");
            await ReloadAreasAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật bàn '{model.TableNumber.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var result = await _tableService.SetTableActiveAsync(id, isActive, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? (isActive ? "Đã kích hoạt bàn." : "Đã ngừng sử dụng bàn.")
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _tableService.DeleteTableAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã xóa mềm bàn." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "Management")]
    public async Task<IActionResult> Areas(CancellationToken cancellationToken = default)
    {
        return View(await _tableService.GetAreasAsync(cancellationToken));
    }

    [Authorize(Policy = "Management")]
    [HttpGet]
    public IActionResult CreateArea()
    {
        return View("AreaForm", new AreaFormViewModel());
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateArea(
        AreaFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View("AreaForm", model);
        }

        var result = await _tableService.CreateAreaAsync(
            new AreaInput(model.Name, model.DisplayOrder, model.IsActive),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm khu vực.");
            return View("AreaForm", model);
        }

        TempData["SuccessMessage"] = $"Đã thêm khu vực '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Areas));
    }

    [Authorize(Policy = "Management")]
    [HttpGet]
    public async Task<IActionResult> EditArea(int id, CancellationToken cancellationToken = default)
    {
        var input = await _tableService.GetAreaInputAsync(id, cancellationToken);
        if (input is null)
        {
            return NotFound();
        }

        return View("AreaForm", new AreaFormViewModel
        {
            Id = id,
            Name = input.Name,
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
        });
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditArea(
        int id,
        AreaFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View("AreaForm", model);
        }

        var result = await _tableService.UpdateAreaAsync(
            id,
            new AreaInput(model.Name, model.DisplayOrder, model.IsActive),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật khu vực.");
            return View("AreaForm", model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật khu vực '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Areas));
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteArea(int id, CancellationToken cancellationToken = default)
    {
        var result = await _tableService.DeleteAreaAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã xóa mềm khu vực." : result.Error;
        return RedirectToAction(nameof(Areas));
    }

    private bool CanManage()
        => User.IsInRole("ADMIN") || User.IsInRole("OWNER") || User.IsInRole("MANAGER");

    private async Task ReloadAreasAsync(DiningTableFormViewModel model, CancellationToken cancellationToken)
    {
        model.Areas = await _tableService.GetAreaOptionsAsync(cancellationToken);
    }
}
