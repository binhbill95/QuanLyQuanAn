namespace RestaurantManagement.Application.Models.Tables;

public sealed record AreaDto(
    int Id,
    string Name,
    int DisplayOrder,
    bool IsActive,
    int TableCount);

public sealed record AreaOptionDto(int Id, string Name, bool IsActive);

public sealed record AreaInput(string Name, int DisplayOrder, bool IsActive);

public sealed record DiningTableDto(
    int Id,
    string TableNumber,
    int? AreaId,
    string? AreaName,
    int MaxSeats,
    bool IsActive,
    string StatusName);

public sealed record DiningTableInput(
    string TableNumber,
    int? AreaId,
    int MaxSeats,
    bool IsActive);
