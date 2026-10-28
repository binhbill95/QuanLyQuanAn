namespace RestaurantManagement.Application.Models.Accounts;

public sealed record AccountListItemDto(
    int UserId,
    string Username,
    int RoleId,
    string RoleCode,
    string RoleName,
    int? EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public sealed record AccountDetailsDto(
    int UserId,
    string Username,
    int RoleId,
    string RoleCode,
    string RoleName,
    int? EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public sealed record AccountEmployeeOptionDto(
    int Id,
    string Code,
    string FullName,
    string PositionName);

public sealed record AccountRoleOptionDto(
    int Id,
    string Code,
    string Name);

public sealed record CreateAccountInput(
    int EmployeeId,
    string Username,
    int RoleId,
    string Password);
