using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Accounts;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>
/// Quản lý tài khoản đăng nhập (AppUser): tạo cho nhân viên, đổi vai trò,
/// đặt lại mật khẩu, khóa/mở. Chỉ ADMIN sử dụng (xem docs/03).
/// </summary>
public interface IAccountService
{
    Task<IReadOnlyList<AccountListItemDto>> SearchAsync(
        string? keyword,
        int? roleId,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<AccountDetailsDto?> GetDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>Nhân viên đang làm việc và **chưa từng** có tài khoản.</summary>
    Task<IReadOnlyList<AccountEmployeeOptionDto>> GetEmployeeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountRoleOptionDto>> GetRoleOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(
        CreateAccountInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateRoleAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ResetPasswordAsync(
        int userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
