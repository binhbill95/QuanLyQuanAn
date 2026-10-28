using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Schedules;
using RestaurantManagement.Application.Models.Shifts;

namespace RestaurantManagement.Application.Abstractions;

/// <summary>Xếp ca làm việc theo ngày (Schedule).</summary>
public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleListItemDto>> GetAsync(
        DateOnly from,
        DateOnly to,
        int? employeeId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(
        ScheduleInput input,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeLookupDto>> GetEmployeeOptionsAsync(
        int? shiftId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftDto>> GetActiveShiftsAsync(CancellationToken cancellationToken = default);
}
