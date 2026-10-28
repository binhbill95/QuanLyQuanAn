namespace RestaurantManagement.Application.Models;

/// <summary>
/// Trạng thái tài khoản tại thời điểm kiểm tra cookie.
/// <see cref="SecurityStamp"/> lấy từ <c>AppUser.UpdatedAt</c>; thay đổi khi khóa/mở,
/// đổi vai trò hoặc đặt lại mật khẩu → phiên cũ bị từ chối.
/// </summary>
public sealed record UserSessionState(
    bool IsActive,
    string? RoleCode,
    long SecurityStamp);
