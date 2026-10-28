using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Menu;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;
    private readonly ICategoryService _categoryService;

    public MenuController(IMenuService menuService, ICategoryService categoryService)
    {
        _menuService = menuService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        int? categoryId,
        bool? isAvailable,
        bool showDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var canManage = CanManageMenu();
        var canToggleAvailability = canManage || User.IsInRole("KITCHEN");

        showDeleted = canManage && showDeleted;

        if (!canToggleAvailability)
        {
            isAvailable = true;
        }

        var model = new MenuIndexViewModel
        {
            Items = await _menuService.GetAsync(
                keyword,
                categoryId,
                isAvailable,
                includeInactiveCategories: canManage,
                showDeleted: showDeleted,
                cancellationToken: cancellationToken),
            Categories = await _categoryService.GetOptionsAsync(
                includeInactive: canManage,
                cancellationToken: cancellationToken),
            Keyword = keyword,
            CategoryId = categoryId,
            IsAvailable = isAvailable,
            ShowDeleted = showDeleted,
            CanManage = canManage,
            CanToggleAvailability = canToggleAvailability,
        };

        return View(model);
    }

    [Authorize(Policy = "Management")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var model = new MenuItemFormViewModel
        {
            Code = await _menuService.SuggestCodeAsync(cancellationToken),
            Categories = await _categoryService.GetOptionsAsync(cancellationToken: cancellationToken),
        };

        return View(model);
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        MenuItemFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await ReloadCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _menuService.CreateAsync(
            new MenuItemInput(
                model.Code,
                model.Name,
                model.CategoryId,
                model.Price,
                model.Unit,
                model.Description,
                model.ImageUrl,
                model.IsAvailable),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm món.");
            await ReloadCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã thêm món '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Index), new { categoryId = model.CategoryId });
    }

    [Authorize(Policy = "Management")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var input = await _menuService.GetInputAsync(id, cancellationToken);

        if (input is null)
        {
            return NotFound();
        }

        return View(new MenuItemFormViewModel
        {
            Id = id,
            Code = input.Code,
            Name = input.Name,
            CategoryId = input.CategoryId,
            Price = input.Price,
            Unit = input.Unit,
            Description = input.Description,
            ImageUrl = input.ImageUrl,
            IsAvailable = input.IsAvailable,
            Categories = await _categoryService.GetOptionsAsync(
                includeInactive: true,
                cancellationToken: cancellationToken),
        });
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        MenuItemFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            await ReloadCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _menuService.UpdateAsync(
            id,
            new MenuItemInput(
                model.Code,
                model.Name,
                model.CategoryId,
                model.Price,
                model.Unit,
                model.Description,
                model.ImageUrl,
                model.IsAvailable),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật món.");
            await ReloadCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật món '{model.Name.Trim()}'.";
        return RedirectToAction(nameof(Index), new { categoryId = model.CategoryId });
    }

    [Authorize(Roles = "ADMIN,OWNER,MANAGER,KITCHEN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAvailable(
        int id,
        bool isAvailable,
        CancellationToken cancellationToken = default)
    {
        var result = await _menuService.SetAvailableAsync(id, isAvailable, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? (isAvailable ? "Đã đánh dấu món còn." : "Đã đánh dấu món hết.")
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _menuService.DeleteAsync(id, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã xóa món." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "Management")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(
        int id,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await _menuService.RestoreAsync(id, categoryId, cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã khôi phục món vào nhóm đang hoạt động."
            : result.Error;

        return RedirectToAction(nameof(Index), new { showDeleted = true });
    }

    private bool CanManageMenu()
        => User.IsInRole("ADMIN") || User.IsInRole("OWNER") || User.IsInRole("MANAGER");

    private async Task ReloadCategoriesAsync(
        MenuItemFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await _categoryService.GetOptionsAsync(
            includeInactive: model.Id.HasValue,
            cancellationToken: cancellationToken);
    }
}
