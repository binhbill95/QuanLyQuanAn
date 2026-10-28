using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Menu;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Catalog;

public sealed class MenuService : IMenuService
{
    private const decimal MaxPrice = 999_999_999m;

    private static readonly Regex CodePattern = new(
        "^[A-Z0-9_]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SuggestedCodePattern = new(
        "^M(\\d+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MenuService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MenuItemListItemDto>> GetAsync(
        string? keyword,
        int? categoryId,
        bool? isAvailable,
        bool includeInactiveCategories = false,
        bool showDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MenuItems
            .AsNoTracking()
            .Where(i => i.IsDeleted == showDeleted);

        if (!includeInactiveCategories)
        {
            query = query.Where(i => i.Category.IsActive && !i.Category.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(i => i.Code.Contains(value) || i.Name.Contains(value));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }

        if (isAvailable.HasValue)
        {
            query = query.Where(i => i.IsAvailable == isAvailable.Value);
        }

        return await query
            .OrderBy(i => i.Category.DisplayOrder)
            .ThenBy(i => i.Category.Name)
            .ThenBy(i => i.Name)
            .Select(i => new MenuItemListItemDto(
                i.Id,
                i.Code,
                i.Name,
                i.CategoryId,
                i.Category.Name,
                i.Category.IsActive,
                i.Category.DisplayOrder,
                i.Price,
                i.Unit,
                i.Description,
                i.ImageUrl,
                i.IsAvailable))
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuItemInput?> GetInputAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.MenuItems
            .AsNoTracking()
            .Where(i => i.Id == id && !i.IsDeleted)
            .Select(i => new MenuItemInput(
                i.Code,
                i.Name,
                i.CategoryId,
                i.Price,
                i.Unit,
                i.Description,
                i.ImageUrl,
                i.IsAvailable))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string> SuggestCodeAsync(CancellationToken cancellationToken = default)
    {
        // Lấy cả bản ghi xóa mềm vì UQ_MenuItem_Code không loại trừ chúng.
        var codes = await _db.MenuItems
            .AsNoTracking()
            .Select(i => i.Code)
            .ToListAsync(cancellationToken);

        var max = 0;

        foreach (var code in codes)
        {
            var match = SuggestedCodePattern.Match(code);

            if (match.Success && int.TryParse(match.Groups[1].Value, out var number) && number > max)
            {
                max = number;
            }
        }

        return $"M{max + 1:000}";
    }

    public async Task<OperationResult<int>> CreateAsync(
        MenuItemInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);

        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var item = new MenuItem
        {
            Code = input.Code.Trim().ToUpperInvariant(),
            Name = input.Name.Trim(),
            CategoryId = input.CategoryId,
            Price = input.Price,
            Unit = Clean(input.Unit),
            Description = Clean(input.Description),
            ImageUrl = Clean(input.ImageUrl),
            IsAvailable = input.IsAvailable,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(item.Id);
    }

    public async Task<OperationResult<bool>> UpdateAsync(
        int id,
        MenuItemInput input,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.MenuItems
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (item is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy món.");
        }

        var error = await ValidateAsync(input, id, cancellationToken);

        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        // Giá mới chỉ áp dụng cho hóa đơn tạo sau; InvoiceDetail.UnitPrice giữ giá lúc bán.
        item.Code = input.Code.Trim().ToUpperInvariant();
        item.Name = input.Name.Trim();
        item.CategoryId = input.CategoryId;
        item.Price = input.Price;
        item.Unit = Clean(input.Unit);
        item.Description = Clean(input.Description);
        item.ImageUrl = Clean(input.ImageUrl);
        item.IsAvailable = input.IsAvailable;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> SetAvailableAsync(
        int id,
        bool isAvailable,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.MenuItems
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (item is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy món.");
        }

        if (item.IsAvailable != isAvailable)
        {
            item.IsAvailable = isAvailable;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync(cancellationToken);
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.MenuItems
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (item is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy món.");
        }

        // Xóa mềm: hóa đơn cũ vẫn tham chiếu được, báo cáo giữ nguyên tên món.
        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> RestoreAsync(
        int id,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.MenuItems
            .FirstOrDefaultAsync(i => i.Id == id && i.IsDeleted, cancellationToken);

        if (item is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy món đã xóa.");
        }

        var categoryExists = await _db.Categories.AnyAsync(
            c => c.Id == categoryId && c.IsActive && !c.IsDeleted,
            cancellationToken);

        if (!categoryExists)
        {
            return OperationResult<bool>.Failure("Vui lòng chọn một nhóm món đang hoạt động.");
        }

        var now = DateTime.UtcNow;
        item.CategoryId = categoryId;
        item.IsDeleted = false;
        item.DeletedAt = null;
        item.UpdatedAt = now;
        item.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    private async Task<string?> ValidateAsync(
        MenuItemInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        if (!CodePattern.IsMatch(code))
        {
            return "Mã món chỉ gồm chữ cái, chữ số, dấu gạch dưới và dài 2–20 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            return "Tên món không được để trống và tối đa 200 ký tự.";
        }

        if (input.Price < 0 || input.Price > MaxPrice)
        {
            return $"Giá bán phải từ 0 đến {MaxPrice:N0}.";
        }

        var categoryExists = await _db.Categories.AnyAsync(
            c => c.Id == input.CategoryId &&
                 !c.IsDeleted &&
                 (c.IsActive ||
                  (excludeId.HasValue &&
                   _db.MenuItems.Any(i => i.Id == excludeId.Value && i.CategoryId == input.CategoryId))),
            cancellationToken);

        if (!categoryExists)
        {
            return "Nhóm món không tồn tại hoặc đã ngừng sử dụng.";
        }

        if (Clean(input.Unit)?.Length > 20)
        {
            return "Đơn vị tính tối đa 20 ký tự.";
        }

        if (Clean(input.Description)?.Length > 500)
        {
            return "Mô tả tối đa 500 ký tự.";
        }

        var imageUrl = Clean(input.ImageUrl);

        if (imageUrl is not null)
        {
            if (imageUrl.Length > 255)
            {
                return "Đường dẫn ảnh tối đa 255 ký tự.";
            }

            var isAbsolute = imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                             imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (!isAbsolute && !imageUrl.StartsWith('/'))
            {
                return "Ảnh phải là URL http(s) hoặc đường dẫn bắt đầu bằng '/'.";
            }
        }

        // UQ_MenuItem_Code áp dụng cả bản ghi xóa mềm.
        var duplicateCode = await _db.MenuItems
            .AnyAsync(i => i.Code == code && (excludeId == null || i.Id != excludeId), cancellationToken);

        if (duplicateCode)
        {
            return $"Mã món '{code}' đã tồn tại.";
        }

        return null;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
