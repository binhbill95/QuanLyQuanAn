namespace RestaurantManagement.Application.Abstractions;

/// <summary>
/// Truy cập thông tin người dùng đang đăng nhập.
/// Triển khai thật ở tầng Web (dựa trên HttpContext).
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }

    string? UserName { get; }

    string? RoleCode { get; }

    /// <summary>Id hồ sơ nhân viên gắn với tài khoản (null với tài khoản ADMIN).</summary>
    int? EmployeeId { get; }

    bool IsAuthenticated { get; }
}
