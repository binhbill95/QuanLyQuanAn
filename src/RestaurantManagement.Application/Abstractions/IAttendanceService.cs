using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Attendance;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>Chấm công cơ bản: check-in, check-out, duyệt công.</summary>
public interface IAttendanceService
{
    /// <summary>
    /// Danh sách ca trong ngày kèm trạng thái chấm công.
    /// <paramref name="employeeId"/> = null → tất cả nhân viên (dành cho quản lý).
    /// </summary>
    Task<IReadOnlyList<AttendanceRowDto>> GetAsync(
        DateOnly date,
        int? employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CheckInAsync(
        int scheduleId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> CheckOutAsync(
        int attendanceId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ApproveAsync(
        int attendanceId,
        int? approverEmployeeId,
        CancellationToken cancellationToken = default);
}
