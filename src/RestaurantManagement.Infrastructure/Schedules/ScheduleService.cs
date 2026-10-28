using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Schedules;
using RestaurantManagement.Application.Models.Shifts;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Schedules;

public sealed class ScheduleService : IScheduleService
{
    private const byte ScheduleStatusApproved = 1;

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ScheduleService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ScheduleListItemDto>> GetAsync(
        DateOnly from,
        DateOnly to,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.WorkDate >= from && s.WorkDate <= to);

        if (employeeId.HasValue)
        {
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        }

        return await query
            .OrderBy(s => s.WorkDate)
            .ThenBy(s => s.Shift.StartTime)
            .Select(s => new ScheduleListItemDto(
                s.Id,
                s.WorkDate,
                s.EmployeeId,
                s.Employee.EmployeeCode,
                s.Employee.FullName,
                s.ShiftId,
                s.Shift.Name,
                s.Shift.StartTime,
                s.Shift.EndTime,
                s.Status,
                s.Attendance != null && !s.Attendance.IsDeleted,
                s.Note))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAsync(
        ScheduleInput input,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (input.WorkDate < today)
        {
            return OperationResult<int>.Failure("Không thể xếp ca cho ngày đã qua.");
        }

        var employee = await _db.Employees
            .Include(e => e.EmployeeShifts)
            .FirstOrDefaultAsync(e => e.Id == input.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee is null)
        {
            return OperationResult<int>.Failure("Không tìm thấy nhân viên.");
        }

        if (employee.Status != 0)
        {
            return OperationResult<int>.Failure("Chỉ xếp ca cho nhân viên đang làm việc.");
        }

        var shift = await _db.Shifts
            .FirstOrDefaultAsync(s => s.Id == input.ShiftId && !s.IsDeleted, cancellationToken);

        if (shift is null)
        {
            return OperationResult<int>.Failure("Không tìm thấy ca làm.");
        }

        if (!shift.IsActive)
        {
            return OperationResult<int>.Failure("Ca làm đã ngừng sử dụng.");
        }

        if (employee.EmployeeShifts.All(es => es.ShiftId != shift.Id))
        {
            return OperationResult<int>.Failure(
                "Nhân viên chưa được gán ca này. Vào “Ca làm → Phân ca nhân viên” để gán trước.");
        }

        // UQ_Schedule_Employee_Shift_Date áp dụng cả bản ghi xóa mềm.
        var existing = await _db.Schedules
            .FirstOrDefaultAsync(
                s => s.EmployeeId == input.EmployeeId &&
                     s.ShiftId == input.ShiftId &&
                     s.WorkDate == input.WorkDate,
                cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            return OperationResult<int>.Failure("Ca này đã được xếp cho nhân viên trong ngày.");
        }

        // Kiểm tra trùng giờ với ca khác trong cùng ngày.
        var sameDayShifts = await _db.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted &&
                        s.EmployeeId == input.EmployeeId &&
                        s.WorkDate == input.WorkDate)
            .Select(s => new { s.Shift.StartTime, s.Shift.EndTime })
            .ToListAsync(cancellationToken);

        var overlaps = sameDayShifts.Any(s => s.StartTime < shift.EndTime && shift.StartTime < s.EndTime);

        if (overlaps)
        {
            return OperationResult<int>.Failure("Ca bị trùng giờ với ca khác của nhân viên trong ngày.");
        }

        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            // Khôi phục bản ghi đã xóa mềm thay vì tạo trùng khóa.
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.Note = Clean(input.Note);
            existing.Status = ScheduleStatusApproved;
            existing.ApprovedBy = _currentUser.EmployeeId;
            existing.ApprovedAt = now;
            existing.UpdatedAt = now;
            existing.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(existing.Id);
        }

        var schedule = new Schedule
        {
            EmployeeId = input.EmployeeId,
            ShiftId = input.ShiftId,
            WorkDate = input.WorkDate,
            Status = ScheduleStatusApproved,
            ApprovedBy = _currentUser.EmployeeId,
            ApprovedAt = now,
            Note = Clean(input.Note),
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
        };

        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(schedule.Id);
    }

    public async Task<OperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _db.Schedules
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (schedule is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy lịch ca.");
        }

        var hasAttendance = await _db.Attendances
            .AnyAsync(a => a.ScheduleId == id && !a.IsDeleted, cancellationToken);

        if (hasAttendance)
        {
            return OperationResult<bool>.Failure("Ca đã có dữ liệu chấm công, không thể xóa.");
        }

        schedule.IsDeleted = true;
        schedule.DeletedAt = DateTime.UtcNow;
        schedule.UpdatedAt = DateTime.UtcNow;
        schedule.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<EmployeeLookupDto>> GetEmployeeOptionsAsync(
        int? shiftId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == 0);

        if (shiftId.HasValue)
        {
            query = query.Where(e => e.EmployeeShifts.Any(es => es.ShiftId == shiftId.Value));
        }

        return await query
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new EmployeeLookupDto(e.Id, e.EmployeeCode, e.FullName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftDto>> GetActiveShiftsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Shifts
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsActive)
            .OrderBy(s => s.StartTime)
            .Select(s => new ShiftDto(s.Id, s.Name, s.StartTime, s.EndTime, s.IsActive, s.EmployeeShifts.Count))
            .ToListAsync(cancellationToken);
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
