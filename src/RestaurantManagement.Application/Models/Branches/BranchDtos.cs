namespace RestaurantManagement.Application.Models.Branches;

public sealed record BranchDto(
    int Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    bool IsActive,
    int AccountCount,
    int EmployeeCount,
    int TableCount);

public sealed record BranchOptionDto(int Id, string Name, bool IsActive);

public sealed record BranchInput(
    string Code,
    string Name,
    string? Address,
    string? Phone,
    bool IsActive);
