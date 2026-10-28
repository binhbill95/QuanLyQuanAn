namespace RestaurantManagement.Application.Models.Employees;

/// <summary>Dòng trong danh sách nhân viên.</summary>
public sealed record EmployeeListItemDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string? Phone,
    byte Status,
    DateOnly StartDate,
    bool HasAccount);

/// <summary>Chi tiết nhân viên.</summary>
public sealed record EmployeeDetailsDto(
    int Id,
    string EmployeeCode,
    string FullName,
    byte? Gender,
    DateOnly? DateOfBirth,
    int PositionId,
    string PositionName,
    string? Phone,
    string? Address,
    string IdentityNumber,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ShiftRate,
    byte Status,
    string? Note,
    IReadOnlyList<string> ShiftNames,
    string? Username,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Dữ liệu dùng cho form Thêm/Sửa.</summary>
public sealed record EmployeeInput(
    string EmployeeCode,
    string FullName,
    byte? Gender,
    DateOnly? DateOfBirth,
    int PositionId,
    string? Phone,
    string? Address,
    string IdentityNumber,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? ShiftRate,
    byte Status,
    string? Note);

/// <summary>Điều kiện tìm kiếm / lọc / phân trang.</summary>
public sealed record EmployeeSearchRequest(
    string? Keyword = null,
    int? PositionId = null,
    byte? Status = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>Vị trí cho dropdown.</summary>
public sealed record PositionLookupDto(
    int Id,
    string Code,
    string Name,
    decimal DefaultShiftRate,
    bool IsActive);
