using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Accounts;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Accounts;

/// <summary>
/// Quản lý tài khoản đăng nhập trên bảng AppUser (không dùng bảng AspNet*).
/// Quy tắc vai trò theo docs/03: chỉ ADMIN được quản lý tài khoản.
/// </summary>
public sealed class AccountService : IAccountService
{
    private const string AdminRoleCode = "ADMIN";
    private const int MinPasswordLength = 6;
    private const int MaxPasswordLength = 100;

    private static readonly Regex UsernamePattern = new(
        "^[a-zA-Z0-9._-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RestaurantDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public AccountService(
        RestaurantDbContext db,
        IPasswordHasher<AppUser> passwordHasher,
        ICurrentUserService currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AccountListItemDto>> SearchAsync(
        string? keyword,
        int? roleId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AppUsers
            .AsNoTracking()
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(u =>
                u.Username.Contains(value) ||
                (u.Employee != null &&
                 (u.Employee.EmployeeCode.Contains(value) || u.Employee.FullName.Contains(value))));
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.Username)
            .Select(u => new AccountListItemDto(
                u.Id,
                u.Username,
                u.RoleId,
                u.Role.Code,
                u.Role.Name,
                u.EmployeeId,
                u.Employee != null ? u.Employee.EmployeeCode : null,
                u.Employee != null ? u.Employee.FullName : null,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountDetailsDto?> GetDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new AccountDetailsDto(
                u.Id,
                u.Username,
                u.RoleId,
                u.Role.Code,
                u.Role.Name,
                u.EmployeeId,
                u.Employee != null ? u.Employee.EmployeeCode : null,
                u.Employee != null ? u.Employee.FullName : null,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountEmployeeOptionDto>> GetEmployeeOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == 0)
            // UX_AppUser_EmployeeId áp dụng cả tài khoản đã xóa mềm.
            .Where(e => !_db.AppUsers.Any(u => u.EmployeeId == e.Id))
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new AccountEmployeeOptionDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Position.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountRoleOptionDto>> GetRoleOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new AccountRoleOptionDto(r.Id, r.Code, r.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAsync(
        CreateAccountInput input,
        CancellationToken cancellationToken = default)
    {
        var username = input.Username.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return OperationResult<int>.Failure("Tên đăng nhập không được để trống.");
        }

        if (username.Length < 3 || username.Length > 50)
        {
            return OperationResult<int>.Failure("Tên đăng nhập phải từ 3 đến 50 ký tự.");
        }

        if (!UsernamePattern.IsMatch(username))
        {
            return OperationResult<int>.Failure(
                "Tên đăng nhập chỉ gồm chữ cái, chữ số và các ký tự . _ -");
        }

        if (input.Password.Length < MinPasswordLength || input.Password.Length > MaxPasswordLength)
        {
            return OperationResult<int>.Failure(
                $"Mật khẩu phải từ {MinPasswordLength} đến {MaxPasswordLength} ký tự.");
        }

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == input.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee is null)
        {
            return OperationResult<int>.Failure("Không tìm thấy nhân viên.");
        }

        if (employee.Status != 0)
        {
            return OperationResult<int>.Failure("Chỉ tạo tài khoản cho nhân viên đang làm việc.");
        }

        var employeeHasAccount = await _db.AppUsers
            .AnyAsync(u => u.EmployeeId == input.EmployeeId, cancellationToken);

        if (employeeHasAccount)
        {
            return OperationResult<int>.Failure("Nhân viên này đã có tài khoản đăng nhập.");
        }

        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == input.RoleId, cancellationToken);

        if (role is null)
        {
            return OperationResult<int>.Failure("Vai trò không hợp lệ.");
        }

        if (string.Equals(role.Code, AdminRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<int>.Failure(
                "Không tạo tài khoản ADMIN qua màn hình này (ADMIN là tài khoản hệ thống).");
        }

        // UQ_AppUser_Username áp dụng cả tài khoản đã xóa mềm.
        var usernameExists = await _db.AppUsers
            .AnyAsync(u => u.Username == username, cancellationToken);

        if (usernameExists)
        {
            return OperationResult<int>.Failure($"Tên đăng nhập '{username}' đã tồn tại.");
        }

        var user = new AppUser
        {
            Username = username,
            RoleId = role.Id,
            EmployeeId = employee.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, input.Password);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(user.Id);
    }

    public async Task<OperationResult<bool>> UpdateRoleAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy tài khoản.");
        }

        if (string.Equals(user.Role.Code, AdminRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.Failure("Không thể đổi vai trò của tài khoản ADMIN.");
        }

        if (user.Id == _currentUser.UserId)
        {
            return OperationResult<bool>.Failure("Không thể tự đổi vai trò của chính mình.");
        }

        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
        {
            return OperationResult<bool>.Failure("Vai trò không hợp lệ.");
        }

        if (string.Equals(role.Code, AdminRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.Failure("Không thể gán vai trò ADMIN qua màn hình này.");
        }

        if (user.RoleId == role.Id)
        {
            return OperationResult<bool>.Success(true);
        }

        user.RoleId = role.Id;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> ResetPasswordAsync(
        int userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (newPassword.Length < MinPasswordLength || newPassword.Length > MaxPasswordLength)
        {
            return OperationResult<bool>.Failure(
                $"Mật khẩu phải từ {MinPasswordLength} đến {MaxPasswordLength} ký tự.");
        }

        var user = await _db.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy tài khoản.");
        }

        if (user.Id == _currentUser.UserId)
        {
            return OperationResult<bool>.Failure(
                "Không đặt lại mật khẩu của chính mình ở đây. Hãy dùng chức năng Đổi mật khẩu.");
        }

        if (string.Equals(user.Role.Code, AdminRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.Failure("Không thể đặt lại mật khẩu tài khoản ADMIN.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy tài khoản.");
        }

        if (user.Id == _currentUser.UserId)
        {
            return OperationResult<bool>.Failure("Không thể khóa tài khoản đang đăng nhập.");
        }

        if (!isActive &&
            string.Equals(user.Role.Code, AdminRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            var otherActiveAdmins = await _db.AppUsers
                .CountAsync(
                    u => u.Id != user.Id &&
                         !u.IsDeleted &&
                         u.IsActive &&
                         u.Role.Code == AdminRoleCode,
                    cancellationToken);

            if (otherActiveAdmins == 0)
            {
                return OperationResult<bool>.Failure(
                    "Không thể khóa ADMIN đang hoạt động cuối cùng của hệ thống.");
            }
        }

        if (user.IsActive == isActive)
        {
            return OperationResult<bool>.Success(true);
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }
}
