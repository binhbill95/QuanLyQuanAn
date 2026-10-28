namespace RestaurantManagement.Application.Models;

/// <summary>
/// Thông tin người dùng sau khi xác thực thành công.
/// Chỉ chứa dữ liệu cần cho việc tạo phiên đăng nhập (claim), không chứa PasswordHash.
/// </summary>
public sealed record AuthenticatedUser(
    int UserId,
    string Username,
    string RoleCode,
    string RoleName,
    int? EmployeeId,
    string? FullName,
    long SecurityStamp);
