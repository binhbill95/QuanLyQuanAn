namespace RestaurantManagement.Application.Models.Audit;

public sealed record AuditActivityFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Action,
    string? Search,
    int Page);

public sealed record AuditActivityDto(
    string Id,
    string TableName,
    string RecordId,
    string Action,
    DateTime ChangedAt,
    string? Actor,
    string? Summary,
    string? OldValues,
    string? NewValues);

public sealed record AuditActivityPage(
    IReadOnlyList<AuditActivityDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
