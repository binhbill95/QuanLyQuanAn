using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Attendance;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Timekeeping;

public sealed class AttendanceService : IAttendanceService
{
    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AttendanceService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AttendanceRowDto>> GetAsync(
        DateOnly date,
        int? employeeId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.WorkDate == date);

        if (employeeId.HasValue)
        {
            query = query.Where(s => s.EmployeeId == employeeId.Value);
        }

        var schedules = await query
            .OrderBy(s => s.Shift.StartTime)
            .Select(s => new
            {
                ScheduleId = s.Id,
                s.EmployeeId,
                EmployeeCode = s.Employee.EmployeeCode,
                EmployeeName = s.Employee.FullName,
                ShiftName = s.Shift.Name,
                s.Shift.StartTime,
                s.Shift.EndTime,
            })
            .ToListAsync(cancellationToken);

        var scheduleIds = schedules.Select(s => s.ScheduleId).ToList();

        var attendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => scheduleIds.Contains(a.ScheduleId) && !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.ScheduleId,
                a.CheckInAt,
                a.CheckOutAt,
                a.Status,
            })
            .ToListAsync(cancellationToken);

        var attendanceBySchedule = attendances.ToDictionary(a => a.ScheduleId);

        return schedules
            .Select(s =>
            {
                attendanceBySchedule.TryGetValue(s.ScheduleId, out var attendance);

                int? workedMinutes = null;

                if (attendance is not null && attendance.CheckOutAt.HasValue)
                {
                    workedMinutes = (int)(attendance.CheckOutAt.Value - attendance.CheckInAt).TotalMinutes;
                }

                return new AttendanceRowDto(
                    s.ScheduleId,
                    attendance?.Id,
                    date,
                    s.EmployeeId,
                    s.EmployeeCode,
                    s.EmployeeName,
                    s.ShiftName,
                    s.StartTime,
                    s.EndTime,
                    attendance?.CheckInAt,
                    attendance?.CheckOutAt,
                    attendance?.Status,
                    workedMinutes);
            })
            .ToList();
    }

    public async Task<OperationResult<int>> CheckInAsync(
        int scheduleId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        var schedule = await _db.Schedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && !s.IsDeleted, cancellationToken);

        if (schedule is null)
        {
            return OperationResult<int>.Failure("Không tìm thấy lịch ca.");
        }

        if (schedule.EmployeeId != employeeId)
        {
            return OperationResult<int>.Failure("Ca này không phải của bạn.");
        }

        if (schedule.WorkDate != today)
        {
            return OperationResult<int>.Failure("Chỉ được chấm công cho ca của ngày hôm nay.");
        }

        var alreadyCheckedIn = await _db.Attendances
            .AnyAsync(a => a.ScheduleId == scheduleId && !a.IsDeleted, cancellationToken);

        if (alreadyCheckedIn)
        {
            return OperationResult<int>.Failure("Ca này đã được chấm công.");
        }

        var now = DateTime.UtcNow;

        var attendance = new Attendance
        {
            ScheduleId = scheduleId,
            CheckInAt = now,
            Status = 0,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
        };

        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(attendance.Id);
    }

    public async Task<OperationResult<bool>> CheckOutAsync(
        int attendanceId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var attendance = await _db.Attendances
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.Id == attendanceId && !a.IsDeleted, cancellationToken);

        if (attendance is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy bản ghi chấm công.");
        }

        if (attendance.Schedule.EmployeeId != employeeId)
        {
            return OperationResult<bool>.Failure("Bản ghi chấm công này không phải của bạn.");
        }

        if (attendance.CheckOutAt.HasValue)
        {
            return OperationResult<bool>.Failure("Ca này đã được check-out.");
        }

        var now = DateTime.UtcNow;

        // CK_Attendance_CheckOut yêu cầu CheckOutAt > CheckInAt (độ chính xác 1 giây).
        if (now <= attendance.CheckInAt)
        {
            now = attendance.CheckInAt.AddMinutes(1);
        }

        attendance.CheckOutAt = now;
        attendance.UpdatedAt = now;
        attendance.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> ApproveAsync(
        int attendanceId,
        int? approverEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.Id == attendanceId && !a.IsDeleted, cancellationToken);

        if (attendance is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy bản ghi chấm công.");
        }

        if (attendance.Status == 1)
        {
            return OperationResult<bool>.Failure("Bản ghi chấm công đã được duyệt.");
        }

        var now = DateTime.UtcNow;

        attendance.Status = 1;
        attendance.ApprovedBy = approverEmployeeId;
        attendance.ApprovedAt = now;
        attendance.UpdatedAt = now;
        attendance.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }
}
