using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Menu;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Catalog;

public sealed class CategoryService : ICategoryService
{
    private static readonly Regex CodePattern = new(
        "^[A-Z0-9_]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CategoryService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Code,
                c.Name,
                c.DisplayOrder,
                c.IsActive,
                c.MenuItems.Count(i => !i.IsDeleted)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryOptionDto>> GetOptionsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryOptionDto(c.Id, c.Code, c.Name, c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryInput?> GetInputAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new CategoryInput(c.Code, c.Name, c.DisplayOrder, c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAsync(
        CategoryInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);

        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var category = new Category
        {
            Code = input.Code.Trim().ToUpperInvariant(),
            Name = input.Name.Trim(),
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(category.Id);
    }

    public async Task<OperationResult<bool>> UpdateAsync(
        int id,
        CategoryInput input,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy nhóm món.");
        }

        var error = await ValidateAsync(input, id, cancellationToken);

        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        category.Code = input.Code.Trim().ToUpperInvariant();
        category.Name = input.Name.Trim();
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy nhóm món.");
        }

        if (category.IsActive != isActive)
        {
            category.IsActive = isActive;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync(cancellationToken);
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy nhóm món.");
        }

        var hasItems = await _db.MenuItems
            .AnyAsync(i => i.CategoryId == id && !i.IsDeleted, cancellationToken);

        if (hasItems)
        {
            return OperationResult<bool>.Failure(
                "Nhóm món vẫn còn món. Hãy chuyển hoặc xóa các món trước.");
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    private async Task<string?> ValidateAsync(
        CategoryInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        if (!CodePattern.IsMatch(code))
        {
            return "Mã nhóm chỉ gồm chữ cái, chữ số, dấu gạch dưới và dài 2–20 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return "Tên nhóm không được để trống và tối đa 100 ký tự.";
        }

        if (input.DisplayOrder < 0)
        {
            return "Thứ tự hiển thị không được âm.";
        }

        // UQ_Category_Code / UQ_Category_Name áp dụng cả bản ghi xóa mềm.
        var duplicateCode = await _db.Categories
            .AnyAsync(c => c.Code == code && (excludeId == null || c.Id != excludeId), cancellationToken);

        if (duplicateCode)
        {
            return $"Mã nhóm '{code}' đã tồn tại.";
        }

        var duplicateName = await _db.Categories
            .AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), cancellationToken);

        if (duplicateName)
        {
            return $"Tên nhóm '{name}' đã tồn tại.";
        }

        return null;
    }
}
