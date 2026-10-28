using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Authentication;

/// <summary>
/// Xác thực trên bảng AppUser/Role có sẵn (không dùng bảng AspNet*).
/// Mật khẩu được băm/kiểm tra bằng <see cref="PasswordHasher{TUser}"/> của ASP.NET Identity (PBKDF2-HMAC-SHA512).
/// </summary>
public sealed class UserAuthenticationService : IUserAuthenticationService
{
    private readonly RestaurantDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public UserAuthenticationService(RestaurantDbContext db, IPasswordHasher<AppUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var normalizedUsername = username.Trim();

        var user = await _db.AppUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(
                u => u.Username == normalizedUsername && !u.IsDeleted,
                cancellationToken);

        // Trả về null cho cả trường hợp không tồn tại / bị khóa / sai mật khẩu
        // để phía UI chỉ hiển thị một thông báo chung, không tiết lộ tài khoản có tồn tại hay không.
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var trackedUser = await _db.AppUsers
                .FirstAsync(u => u.Id == user.Id, cancellationToken);

            trackedUser.PasswordHash = _passwordHasher.HashPassword(trackedUser, password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Cập nhật LastLoginAt bằng ExecuteUpdate để không cần load lại entity.
        await _db.AppUsers
            .Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.LastLoginAt, DateTime.UtcNow),
                cancellationToken);

        return new AuthenticatedUser(
            user.Id,
            user.Username,
            user.Role.Code,
            user.Role.Name,
            user.EmployeeId,
            user.Employee?.FullName,
            ToSecurityStamp(user.UpdatedAt));
    }

    public async Task<UserSessionState?> GetSessionStateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var state = await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new
            {
                u.IsActive,
                u.UpdatedAt,
                RoleCode = u.Role.Code,
                EmployeeUsable = u.EmployeeId == null ||
                                 (u.Employee != null && !u.Employee.IsDeleted && u.Employee.Status == 0),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (state is null)
        {
            return null;
        }

        return new UserSessionState(
            state.IsActive && state.EmployeeUsable,
            state.RoleCode,
            ToSecurityStamp(state.UpdatedAt));
    }

    /// <summary>
    /// Dấu phiên: thay đổi khi khóa/mở, đổi vai trò hoặc đặt lại mật khẩu.
    /// <c>LastLoginAt</c> được cập nhật riêng nên không làm mất hiệu lực phiên.
    /// </summary>
    private static long ToSecurityStamp(DateTime? updatedAt) => updatedAt?.Ticks ?? 0;

    public async Task<bool> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            return false;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);

        if (verification == PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
