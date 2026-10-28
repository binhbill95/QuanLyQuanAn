using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Employees;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>
/// Nghiệp vụ hồ sơ nhân viên (Phase 4).
/// Xóa là xóa mềm (IsDeleted) để bảo toàn dữ liệu chấm công / hóa đơn / báo cáo.
/// </summary>
public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> SearchAsync(
        EmployeeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<EmployeeInput?> GetInputAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(
        EmployeeInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateAsync(
        int id,
        EmployeeInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PositionLookupDto>> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gợi ý mã kế tiếp dạng NV001, NV002…</summary>
    Task<string> SuggestEmployeeCodeAsync(CancellationToken cancellationToken = default);
}
