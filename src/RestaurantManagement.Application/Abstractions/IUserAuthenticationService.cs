using RestaurantManagement.Application.Models;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>
/// Xác thực người dùng dựa trên bảng AppUser/Role.
/// Triển khai ở tầng Infrastructure (EF Core + PasswordHasher của ASP.NET Identity).
/// Tên đặt khác <c>Microsoft.AspNetCore.Authentication.IAuthenticationService</c> để tránh xung đột.
/// </summary>
public interface IUserAuthenticationService
{
    /// <summary>
    /// Kiểm tra tên đăng nhập / mật khẩu. Trả về null nếu sai, tài khoản bị khóa hoặc đã xóa.
    /// Cập nhật LastLoginAt khi thành công.
    /// </summary>
    Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi mật khẩu: phải xác thực đúng mật khẩu hiện tại.
    /// Trả về false nếu không tìm thấy người dùng hoặc mật khẩu hiện tại sai.
    /// </summary>
    Task<bool> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Trạng thái hiện tại của tài khoản để kiểm tra lại cookie mỗi request.
    /// Trả về null nếu tài khoản đã bị xóa cứng.
    /// </summary>
    Task<UserSessionState?> GetSessionStateAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
