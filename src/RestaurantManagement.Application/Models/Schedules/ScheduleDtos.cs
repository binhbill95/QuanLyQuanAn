namespace RestaurantManagement.Application.Models.Schedules;

public sealed record ScheduleListItemDto(
    int Id,
    DateOnly WorkDate,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    int ShiftId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    byte Status,
    bool HasAttendance,
    string? Note);

public sealed record ScheduleInput(
    int EmployeeId,
    int ShiftId,
    DateOnly WorkDate,
    string? Note);
