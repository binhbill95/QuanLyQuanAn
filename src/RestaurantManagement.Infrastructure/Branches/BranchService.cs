using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Branches;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Branches;

public sealed class BranchService : IBranchService
{
    private static readonly Regex CodePattern = new(
        "^[A-Za-z0-9_-]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BranchService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<BranchDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Branches
            .AsNoTracking()
            .Where(branch => !branch.IsDeleted)
            .OrderBy(branch => branch.Name)
            .Select(branch => new BranchDto(
                branch.Id,
                branch.Code,
                branch.Name,
                branch.Address,
                branch.Phone,
                branch.IsActive,
                branch.AppUsers.Count(user => !user.IsDeleted),
                branch.Employees.Count(employee => !employee.IsDeleted),
                branch.DiningTables.Count(table => !table.IsDeleted)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BranchOptionDto>> GetOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var query = _db.Branches
            .AsNoTracking()
            .Where(branch => !branch.IsDeleted && branch.IsActive);
        if (_currentUser.BranchId.HasValue)
        {
            query = query.Where(branch => branch.Id == _currentUser.BranchId.Value);
        }

        return await query
            .OrderBy(branch => branch.Name)
            .Select(branch => new BranchOptionDto(branch.Id, branch.Name, branch.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchInput?> GetInputAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Branches
            .AsNoTracking()
            .Where(branch => branch.Id == id && !branch.IsDeleted)
            .Select(branch => new BranchInput(
                branch.Code,
                branch.Name,
                branch.Address,
                branch.Phone,
                branch.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAsync(
        BranchInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);
        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var branch = new Branch
        {
            Code = input.Code.Trim().ToUpperInvariant(),
            Name = input.Name.Trim(),
            Address = Clean(input.Address),
            Phone = Clean(input.Phone),
            IsActive = input.IsActive,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
        };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(branch.Id);
    }

    public async Task<OperationResult<bool>> UpdateAsync(
        int id,
        BranchInput input,
        CancellationToken cancellationToken = default)
    {
        var branch = await _db.Branches
            .FirstOrDefaultAsync(candidate => candidate.Id == id && !candidate.IsDeleted, cancellationToken);
        if (branch is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy chi nhánh.");
        }

        var error = await ValidateAsync(input, id, cancellationToken);
        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        if (!input.IsActive && branch.IsActive &&
            (await _db.DiningTables.AnyAsync(table =>
                 table.BranchId == id && table.IsActive && !table.IsDeleted, cancellationToken) ||
             await _db.Employees.AnyAsync(employee =>
                 employee.BranchId == id && employee.Status == 0 && !employee.IsDeleted, cancellationToken) ||
             await _db.AppUsers.AnyAsync(user =>
                 user.BranchId == id && user.IsActive && !user.IsDeleted, cancellationToken)))
        {
            return OperationResult<bool>.Failure(
                "Hãy ngừng sử dụng các bàn, cho nhân viên nghỉ và khóa tài khoản thuộc chi nhánh trước.");
        }

        var now = DateTime.UtcNow;
        var branchIdentityChanged =
            !string.Equals(branch.Name, input.Name.Trim(), StringComparison.Ordinal) ||
            !string.Equals(branch.Code, input.Code.Trim(), StringComparison.OrdinalIgnoreCase);
        branch.Code = input.Code.Trim().ToUpperInvariant();
        branch.Name = input.Name.Trim();
        branch.Address = Clean(input.Address);
        branch.Phone = Clean(input.Phone);
        branch.IsActive = input.IsActive;
        branch.UpdatedAt = now;
        branch.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);

        if (branchIdentityChanged)
        {
            await _db.AppUsers
                .Where(user => user.BranchId == id && !user.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(user => user.UpdatedAt, now)
                        .SetProperty(user => user.UpdatedBy, _currentUser.UserId),
                    cancellationToken);
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var branch = await _db.Branches
            .FirstOrDefaultAsync(candidate => candidate.Id == id && !candidate.IsDeleted, cancellationToken);
        if (branch is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy chi nhánh.");
        }

        if (await _db.AppUsers.AnyAsync(user => user.BranchId == id && !user.IsDeleted, cancellationToken) ||
            await _db.Employees.AnyAsync(employee => employee.BranchId == id && !employee.IsDeleted, cancellationToken) ||
            await _db.DiningTables.AnyAsync(table => table.BranchId == id && !table.IsDeleted, cancellationToken) ||
            await _db.Invoices.AnyAsync(invoice => invoice.BranchId == id && !invoice.IsDeleted, cancellationToken))
        {
            return OperationResult<bool>.Failure(
                "Không thể xóa chi nhánh còn tài khoản, bàn hoặc hóa đơn. Có thể ngừng sử dụng chi nhánh.");
        }

        var now = DateTime.UtcNow;
        branch.IsDeleted = true;
        branch.DeletedAt = now;
        branch.UpdatedAt = now;
        branch.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private async Task<string?> ValidateAsync(
        BranchInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();
        if (!CodePattern.IsMatch(code))
        {
            return "Mã chi nhánh phải gồm 2–20 ký tự chữ, số, gạch ngang hoặc gạch dưới.";
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return "Tên chi nhánh không được để trống và tối đa 100 ký tự.";
        }

        if (input.Address?.Trim().Length > 255 || input.Phone?.Trim().Length > 20)
        {
            return "Địa chỉ tối đa 255 ký tự và số điện thoại tối đa 20 ký tự.";
        }

        if (await _db.Branches.AnyAsync(
                branch => branch.Code == code && (excludeId == null || branch.Id != excludeId),
                cancellationToken))
        {
            return $"Mã chi nhánh '{code}' đã tồn tại.";
        }

        return null;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
