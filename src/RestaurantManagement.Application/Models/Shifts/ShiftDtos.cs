namespace RestaurantManagement.Application.Models.Shifts;

public sealed record ShiftDto(
    int Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive,
    int AssignedEmployees);

public sealed record ShiftInput(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public sealed record EmployeeShiftAssignmentDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    IReadOnlyList<int> ShiftIds,
    IReadOnlyList<string> ShiftNames);

public sealed record EmployeeLookupDto(
    int Id,
    string Code,
    string FullName);
