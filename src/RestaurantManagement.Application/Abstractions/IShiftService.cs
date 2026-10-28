using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Shifts;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>
/// Quản lý ca làm việc (Shift) và ca mà từng nhân viên có thể làm (EmployeeShift).
/// </summary>
public interface IShiftService
{
    Task<IReadOnlyList<ShiftDto>> GetAllAsync(bool includeInactive = true, CancellationToken cancellationToken = default);

    Task<ShiftInput?> GetInputAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(ShiftInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> UpdateAsync(int id, ShiftInput input, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeShiftAssignmentDto>> GetAssignmentsAsync(
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SetEmployeeShiftsAsync(
        int employeeId,
        IReadOnlyList<int> shiftIds,
        CancellationToken cancellationToken = default);
}
