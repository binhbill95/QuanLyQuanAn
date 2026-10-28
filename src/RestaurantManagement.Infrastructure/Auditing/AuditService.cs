using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Audit;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private const int PageSize = 25;
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    private readonly RestaurantDbContext _db;

    public AuditService(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<AuditActivityPage> GetActivitiesAsync(
        AuditActivityFilter filter,
        CancellationToken cancellationToken = default)
    {
        var auditQuery = _db.AuditLogs.AsNoTracking().AsQueryable();
        var transferQuery = _db.TableTransferLogs
            .AsNoTracking()
            .Where(log => !log.IsDeleted);

        if (filter.FromDate is { } fromDate)
        {
            var fromUtc = ToUtcStartOfDay(fromDate);
            auditQuery = auditQuery.Where(log => log.ChangedAt >= fromUtc);
            transferQuery = transferQuery.Where(log => log.MovedAt >= fromUtc);
        }

        if (filter.ToDate is { } toDate && toDate < DateOnly.MaxValue)
        {
            var untilUtc = ToUtcStartOfDay(toDate.AddDays(1));
            auditQuery = auditQuery.Where(log => log.ChangedAt < untilUtc);
            transferQuery = transferQuery.Where(log => log.MovedAt < untilUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim();
            if (action is "INSERT" or "UPDATE" or "DELETE")
            {
                auditQuery = auditQuery.Where(log => log.Action == action);
                transferQuery = transferQuery.Where(_ => false);
            }
            else if (action is "Chuyển bàn" or "Gộp bàn")
            {
                auditQuery = auditQuery.Where(_ => false);
                transferQuery = action == "Gộp bàn"
                    ? transferQuery.Where(log => log.Note != null && log.Note.StartsWith("Gộp hóa đơn"))
                    : transferQuery.Where(log => log.Note == null || !log.Note.StartsWith("Gộp hóa đơn"));
            }
            else
            {
                auditQuery = auditQuery.Where(_ => false);
                transferQuery = transferQuery.Where(_ => false);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            auditQuery = auditQuery.Where(log =>
                log.TableName.Contains(search) ||
                log.RecordId.Contains(search) ||
                (log.ChangedByUser != null && log.ChangedByUser.Username.Contains(search)));
            transferQuery = transferQuery.Where(log =>
                log.Invoice.InvoiceCode.Contains(search) ||
                log.FromTable.TableNumber.Contains(search) ||
                log.ToTable.TableNumber.Contains(search) ||
                log.MovedByEmployee.FullName.Contains(search) ||
                (log.Note != null && log.Note.Contains(search)));
        }

        var auditCount = await auditQuery.CountAsync(cancellationToken);
        var transferCount = await transferQuery.CountAsync(cancellationToken);
        var totalCount = auditCount + transferCount;
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var page = Math.Clamp(filter.Page, 1, pageCount);
        var numberToFetch = page * PageSize;
        var auditItems = await auditQuery
            .OrderByDescending(log => log.ChangedAt)
            .ThenByDescending(log => log.Id)
            .Take(numberToFetch)
            .Select(log => new AuditActivityDto(
                "A-" + log.Id.ToString(),
                log.TableName,
                log.RecordId,
                log.Action,
                log.ChangedAt,
                log.ChangedByUser == null ? null : log.ChangedByUser.Username,
                null,
                log.OldValues,
                log.NewValues))
            .ToListAsync(cancellationToken);
        var transferItems = await transferQuery
            .OrderByDescending(log => log.MovedAt)
            .ThenByDescending(log => log.Id)
            .Take(numberToFetch)
            .Select(log => new AuditActivityDto(
                "T-" + log.Id.ToString(),
                "TableTransferLog",
                log.Invoice.InvoiceCode,
                log.Note != null && log.Note.StartsWith("Gộp hóa đơn")
                    ? "Gộp bàn"
                    : "Chuyển bàn",
                log.MovedAt,
                log.MovedByEmployee.FullName,
                log.FromTable.TableNumber + " → " + log.ToTable.TableNumber +
                (log.Note == null ? string.Empty : " · " + log.Note),
                null,
                null))
            .ToListAsync(cancellationToken);
        var items = auditItems
            .Concat(transferItems)
            .OrderByDescending(activity => activity.ChangedAt)
            .ThenByDescending(activity => activity.Id, StringComparer.Ordinal)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(activity => activity with
            {
                ChangedAt = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(activity.ChangedAt, DateTimeKind.Utc),
                    VietnamTimeZone),
            })
            .ToList();

        return new AuditActivityPage(items, page, PageSize, totalCount);
    }

    private static DateTime ToUtcStartOfDay(DateOnly date)
    {
        var localDate = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localDate, VietnamTimeZone);
    }
}
