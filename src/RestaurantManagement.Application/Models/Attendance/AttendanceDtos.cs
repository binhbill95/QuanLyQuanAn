namespace RestaurantManagement.Application.Models.Attendance;

public sealed record AttendanceRowDto(
    int ScheduleId,
    int? AttendanceId,
    DateOnly WorkDate,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateTime? CheckInAt,
    DateTime? CheckOutAt,
    byte? Status,
    int? WorkedMinutes);
