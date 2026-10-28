using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Menu;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "Management")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryService.GetAllAsync(true, cancellationToken);
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CategoryFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.CreateAsync(
            new CategoryInput(model.Code, model.Name, model.DisplayOrder, model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm nhóm món.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã thêm nhóm '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var input = await _categoryService.GetInputAsync(id, cancellationToken);

        if (input is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = id,
            Code = input.Code,
            Name = input.Name,
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CategoryFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(
            id,
            new CategoryInput(model.Code, model.Name, model.DisplayOrder, model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật nhóm món.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật nhóm '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.SetActiveAsync(id, isActive, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? (isActive ? "Đã kích hoạt nhóm món." : "Đã ngừng sử dụng nhóm món.")
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.DeleteAsync(id, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã xóa nhóm món." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
