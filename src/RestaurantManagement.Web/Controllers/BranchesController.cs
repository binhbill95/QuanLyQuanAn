using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class BranchesController : Controller
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        return View(await _branchService.GetAllAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new BranchFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        BranchFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var result = await _branchService.CreateAsync(model.ToInput(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo chi nhánh.");
            return View("Form", model);
        }

        TempData["SuccessMessage"] = $"Đã tạo chi nhánh '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var input = await _branchService.GetInputAsync(id, cancellationToken);
        return input is null ? NotFound() : View("Form", BranchFormViewModel.From(input, id));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        BranchFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var result = await _branchService.UpdateAsync(id, model.ToInput(), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật chi nhánh.");
            return View("Form", model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật chi nhánh '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _branchService.DeleteAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã xóa mềm chi nhánh." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
