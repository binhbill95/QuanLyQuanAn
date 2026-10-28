namespace RestaurantManagement.Application.Models.Menu;

public sealed record CategoryDto(
    int Id,
    string Code,
    string Name,
    int DisplayOrder,
    bool IsActive,
    int ItemCount);

public sealed record CategoryInput(
    string Code,
    string Name,
    int DisplayOrder,
    bool IsActive);

public sealed record CategoryOptionDto(
    int Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record MenuItemListItemDto(
    int Id,
    string Code,
    string Name,
    int CategoryId,
    string CategoryName,
    bool CategoryIsActive,
    int CategoryDisplayOrder,
    decimal Price,
    string? Unit,
    string? Description,
    string? ImageUrl,
    bool IsAvailable);

public sealed record MenuItemInput(
    string Code,
    string Name,
    int CategoryId,
    decimal Price,
    string? Unit,
    string? Description,
    string? ImageUrl,
    bool IsAvailable);
