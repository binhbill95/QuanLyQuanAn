using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Orders;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Orders;

public sealed class OrderService : IOrderService
{
    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrderService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ActiveTableOrderDto>> GetTablesAsync(
        CancellationToken cancellationToken = default)
    {
        var statusRows = await _db.VwDiningTableStatuses
            .AsNoTracking()
            .OrderBy(t => t.AreaName)
            .ThenBy(t => t.TableNumber)
            .ToListAsync(cancellationToken);

        var invoiceIds = statusRows
            .Where(t => t.InvoiceId.HasValue)
            .Select(t => t.InvoiceId!.Value)
            .Distinct()
            .ToList();
        var invoiceCodes = await _db.Invoices
            .AsNoTracking()
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.InvoiceCode, cancellationToken);

        return statusRows.Select(t => new ActiveTableOrderDto(
                t.TableId,
                t.TableNumber,
                t.AreaName,
                t.MaxSeats,
                t.StatusName,
                t.InvoiceId,
                t.InvoiceId.HasValue && invoiceCodes.TryGetValue(t.InvoiceId.Value, out var code) ? code : null))
            .ToList();
    }

    public async Task<IReadOnlyList<OrderMenuOptionDto>> GetMenuAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.MenuItems
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsAvailable && m.Category.IsActive && !m.Category.IsDeleted)
            .OrderBy(m => m.Category.DisplayOrder)
            .ThenBy(m => m.Category.Name)
            .ThenBy(m => m.Name)
            .Select(m => new OrderMenuOptionDto(
                m.Id,
                m.Code,
                m.Name,
                m.Category.Name,
                m.Price,
                m.Unit))
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceOrderDto?> GetInvoiceAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoiceId && i.Status == 0 && !i.IsDeleted)
            .Select(i => new
            {
                i.Id,
                i.InvoiceCode,
                i.CustomerCount,
                i.OpenedAt,
                Tables = i.InvoiceTables
                    .Where(t => t.ReleasedAt == null && !t.IsDeleted)
                    .OrderByDescending(t => t.IsPrimary)
                    .ThenBy(t => t.Table.TableNumber)
                    .Select(t => new OrderTableDto(t.TableId, t.Table.TableNumber, t.IsPrimary))
                    .ToList(),
                Items = i.InvoiceDetails
                    .Where(d => !d.IsDeleted)
                    .OrderBy(d => d.OrderedAt)
                    .ThenBy(d => d.Id)
                    .Select(d => new OrderLineDto(
                        d.Id,
                        d.MenuItemId,
                        d.MenuItem.Code,
                        d.MenuItem.Name,
                        d.Quantity,
                        d.UnitPrice,
                        d.MenuItem.Unit,
                        d.Note,
                        d.Status,
                        d.OrderedAt,
                        d.CancelReason))
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        return new InvoiceOrderDto(
            invoice.Id,
            invoice.InvoiceCode,
            invoice.CustomerCount,
            invoice.OpenedAt,
            invoice.Tables,
            invoice.Items,
            invoice.Items.Where(i => i.Status != 3).Sum(i => i.Quantity * i.UnitPrice));
    }

    public async Task<IReadOnlyList<ActiveInvoiceDto>> GetActiveInvoicesAsync(
        CancellationToken cancellationToken = default)
    {
        var invoices = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == 0 && !i.IsDeleted)
            .OrderBy(i => i.OpenedAt)
            .Select(i => new
            {
                i.Id,
                i.InvoiceCode,
                i.CustomerCount,
                i.OpenedAt,
                Tables = i.InvoiceTables
                    .Where(t => t.ReleasedAt == null && !t.IsDeleted)
                    .OrderByDescending(t => t.IsPrimary)
                    .ThenBy(t => t.Table.TableNumber)
                    .Select(t => new OrderTableDto(t.TableId, t.Table.TableNumber, t.IsPrimary))
                    .ToList(),
                Subtotal = i.InvoiceDetails
                    .Where(d => !d.IsDeleted && d.Status != 3)
                    .Sum(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        return invoices.Select(i => new ActiveInvoiceDto(
                i.Id,
                i.InvoiceCode,
                i.CustomerCount,
                i.OpenedAt,
                i.Tables,
                i.Subtotal))
            .ToList();
    }

    public async Task<IReadOnlyList<KitchenQueueItemDto>> GetKitchenQueueAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.VwKitchenQueues
            .AsNoTracking()
            .OrderBy(q => q.OrderedAt)
            .ThenBy(q => q.InvoiceDetailId)
            .Select(q => new KitchenQueueItemDto(
                q.InvoiceDetailId,
                q.InvoiceId,
                q.InvoiceCode,
                q.TableNumber,
                q.ItemCode,
                q.ItemName,
                q.Quantity,
                q.Note,
                q.Status,
                q.OrderedAt,
                q.WaitingMinutes))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> OpenInvoiceAsync(
        OpenInvoiceInput input,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        if (input.CustomerCount is < 1 or > 100)
        {
            return OperationResult<int>.Failure("Số lượng khách phải từ 1 đến 100.");
        }

        return await ExecuteTransactionAsync(
            async token =>
            {
                var employeeExists = await _db.Employees.AnyAsync(
                    e => e.Id == employeeId && e.Status == 0 && !e.IsDeleted,
                    token);
                if (!employeeExists)
                {
                    return OperationResult<int>.Failure("Hồ sơ nhân viên không còn hoạt động.");
                }

                var table = await _db.DiningTables
                    .FirstOrDefaultAsync(t => t.Id == input.TableId && t.IsActive && !t.IsDeleted, token);
                if (table is null)
                {
                    return OperationResult<int>.Failure("Bàn không tồn tại hoặc đã ngừng sử dụng.");
                }

                if (input.CustomerCount > table.MaxSeats)
                {
                    return OperationResult<int>.Failure($"Bàn chỉ phục vụ tối đa {table.MaxSeats} khách.");
                }

                var isOccupied = await _db.InvoiceTables.AnyAsync(
                    t => t.TableId == input.TableId && t.ReleasedAt == null && !t.IsDeleted,
                    token);
                if (isOccupied)
                {
                    return OperationResult<int>.Failure("Bàn đang có hóa đơn phục vụ.");
                }

                var localTime = TimeOnly.FromDateTime(DateTime.Now);
                var matchingShiftIds = await _db.Shifts
                    .Where(s => s.IsActive &&
                                !s.IsDeleted &&
                                s.StartTime <= localTime &&
                                s.EndTime > localTime)
                    .OrderBy(s => s.StartTime)
                    .Select(s => s.Id)
                    .Take(2)
                    .ToListAsync(token);
                if (matchingShiftIds.Count > 1)
                {
                    return OperationResult<int>.Failure(
                        "Không thể mở hóa đơn vì nhiều ca làm đang trùng khung giờ hiện tại.");
                }

                var now = DateTime.UtcNow;
                var invoice = new Invoice
                {
                    InvoiceCode = CreateInvoiceCode(),
                    CustomerCount = input.CustomerCount,
                    OpenedByEmployeeId = employeeId,
                    OpenedAt = now,
                    ShiftId = matchingShiftIds.Count == 1 ? matchingShiftIds[0] : null,
                    Status = 0,
                    CreatedAt = now,
                    CreatedBy = _currentUser.UserId,
                };
                var invoiceTable = new InvoiceTable
                {
                    Invoice = invoice,
                    TableId = table.Id,
                    AssignedAt = now,
                    IsPrimary = true,
                    CreatedAt = now,
                    CreatedBy = _currentUser.UserId,
                };

                _db.Invoices.Add(invoice);
                _db.InvoiceTables.Add(invoiceTable);
                await _db.SaveChangesAsync(token);
                return OperationResult<int>.Success(invoice.Id);
            },
            IsolationLevel.Serializable,
            cancellationToken);
    }

    public async Task<OperationResult<bool>> AddItemAsync(
        int invoiceId,
        AddOrderItemInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.Quantity is < 1 or > 1000)
        {
            return OperationResult<bool>.Failure("Số lượng món phải từ 1 đến 1000.");
        }

        var note = Clean(input.Note);
        if (note?.Length > 255)
        {
            return OperationResult<bool>.Failure("Ghi chú món tối đa 255 ký tự.");
        }

        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Status == 0 && !i.IsDeleted, cancellationToken);
        if (invoice is null)
        {
            return OperationResult<bool>.Failure("Hóa đơn không còn ở trạng thái phục vụ.");
        }

        var menuItem = await _db.MenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == input.MenuItemId &&
                     !m.IsDeleted &&
                     m.IsAvailable &&
                     m.Category.IsActive &&
                     !m.Category.IsDeleted,
                cancellationToken);
        if (menuItem is null)
        {
            return OperationResult<bool>.Failure("Món không tồn tại, đã hết hoặc nhóm món đã ngừng.");
        }

        var now = DateTime.UtcNow;
        _db.InvoiceDetails.Add(new InvoiceDetail
        {
            InvoiceId = invoiceId,
            MenuItemId = menuItem.Id,
            Quantity = input.Quantity,
            UnitPrice = menuItem.Price,
            Note = note,
            Status = 0,
            OrderedAt = now,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
        });

        invoice.UpdatedAt = now;
        invoice.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> CancelItemAsync(
        int invoiceId,
        int detailId,
        string reason,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var cancelReason = Clean(reason);
        if (cancelReason is null || cancelReason.Length > 255)
        {
            return OperationResult<bool>.Failure("Vui lòng nhập lý do hủy món, tối đa 255 ký tự.");
        }

        var detail = await _db.InvoiceDetails
            .Include(d => d.Invoice)
            .FirstOrDefaultAsync(
                d => d.Id == detailId &&
                     d.InvoiceId == invoiceId &&
                     !d.IsDeleted &&
                     d.Invoice.Status == 0 &&
                     !d.Invoice.IsDeleted,
                cancellationToken);
        if (detail is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy món trong hóa đơn đang phục vụ.");
        }

        if (detail.Status != 0)
        {
            return OperationResult<bool>.Failure("Chỉ được hủy món đang chờ bếp xử lý.");
        }

        var now = DateTime.UtcNow;
        detail.Status = 3;
        detail.CancelledAt = now;
        detail.CancelReason = cancelReason;
        detail.CancelledBy = employeeId;
        detail.UpdatedAt = now;
        detail.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> ReplaceItemAsync(
        int invoiceId,
        int detailId,
        AddOrderItemInput replacement,
        string reason,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        if (replacement.Quantity is < 1 or > 1000)
        {
            return OperationResult<bool>.Failure("Số lượng món phải từ 1 đến 1000.");
        }

        var cancelReason = Clean(reason);
        var note = Clean(replacement.Note);
        if (cancelReason is null || cancelReason.Length > 255)
        {
            return OperationResult<bool>.Failure("Vui lòng nhập lý do đổi món, tối đa 255 ký tự.");
        }

        if (note?.Length > 255)
        {
            return OperationResult<bool>.Failure("Ghi chú món tối đa 255 ký tự.");
        }

        return await ExecuteTransactionAsync(
            async token =>
            {
                var detail = await _db.InvoiceDetails
                    .Include(d => d.Invoice)
                    .FirstOrDefaultAsync(
                        d => d.Id == detailId &&
                             d.InvoiceId == invoiceId &&
                             !d.IsDeleted &&
                             d.Invoice.Status == 0 &&
                             !d.Invoice.IsDeleted,
                        token);
                if (detail is null)
                {
                    return OperationResult<bool>.Failure("Không tìm thấy món trong hóa đơn đang phục vụ.");
                }

                if (detail.Status != 0)
                {
                    return OperationResult<bool>.Failure("Chỉ được đổi món đang chờ bếp xử lý.");
                }

                var menuItem = await _db.MenuItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.Id == replacement.MenuItemId &&
                             !m.IsDeleted &&
                             m.IsAvailable &&
                             m.Category.IsActive &&
                             !m.Category.IsDeleted,
                        token);
                if (menuItem is null)
                {
                    return OperationResult<bool>.Failure("Món thay thế không tồn tại, đã hết hoặc nhóm món đã ngừng.");
                }

                var now = DateTime.UtcNow;
                detail.Status = 3;
                detail.CancelledAt = now;
                detail.CancelReason = cancelReason;
                detail.CancelledBy = employeeId;
                detail.UpdatedAt = now;
                detail.UpdatedBy = _currentUser.UserId;

                _db.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoiceId,
                    MenuItemId = menuItem.Id,
                    Quantity = replacement.Quantity,
                    UnitPrice = menuItem.Price,
                    Note = note,
                    Status = 0,
                    OrderedAt = now,
                    CreatedAt = now,
                    CreatedBy = _currentUser.UserId,
                });

                detail.Invoice.UpdatedAt = now;
                detail.Invoice.UpdatedBy = _currentUser.UserId;
                await _db.SaveChangesAsync(token);
                return OperationResult<bool>.Success(true);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    public async Task<OperationResult<bool>> SetKitchenStatusAsync(
        int detailId,
        byte status,
        CancellationToken cancellationToken = default)
    {
        if (status is not (1 or 2))
        {
            return OperationResult<bool>.Failure("Trạng thái bếp không hợp lệ.");
        }

        var detail = await _db.InvoiceDetails
            .Include(d => d.Invoice)
            .FirstOrDefaultAsync(
                d => d.Id == detailId &&
                     !d.IsDeleted &&
                     d.Invoice.Status == 0 &&
                     !d.Invoice.IsDeleted,
                cancellationToken);
        if (detail is null)
        {
            return OperationResult<bool>.Failure("Món không còn trong hàng đợi bếp.");
        }

        var expectedStatus = status == 1 ? (byte)0 : (byte)1;
        if (detail.Status != expectedStatus)
        {
            return OperationResult<bool>.Failure(
                status == 1
                    ? "Chỉ món đang chờ mới có thể chuyển sang đang chế biến."
                    : "Chỉ món đang chế biến mới có thể đánh dấu hoàn thành.");
        }

        var now = DateTime.UtcNow;
        detail.Status = status;
        detail.CompletedAt = status == 2 ? now : null;
        detail.UpdatedAt = now;
        detail.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> MoveTableAsync(
        int invoiceId,
        int fromTableId,
        int toTableId,
        int employeeId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (fromTableId == toTableId)
        {
            return OperationResult<bool>.Failure("Bàn chuyển đến phải khác bàn hiện tại.");
        }

        var cleanNote = Clean(note);
        if (cleanNote?.Length > 255)
        {
            return OperationResult<bool>.Failure("Ghi chú chuyển bàn tối đa 255 ký tự.");
        }

        return await ExecuteTransactionAsync(
            async token =>
            {

                var invoiceIsOpen = await _db.Invoices.AnyAsync(
                    i => i.Id == invoiceId && i.Status == 0 && !i.IsDeleted,
                    token);
                if (!invoiceIsOpen)
                {
                    return OperationResult<bool>.Failure("Hóa đơn không còn ở trạng thái phục vụ.");
                }

                var source = await _db.InvoiceTables.FirstOrDefaultAsync(
                    t => t.InvoiceId == invoiceId &&
                         t.TableId == fromTableId &&
                         t.ReleasedAt == null &&
                         !t.IsDeleted,
                    token);
                if (source is null)
                {
                    return OperationResult<bool>.Failure("Bàn nguồn không thuộc hóa đơn đang phục vụ.");
                }

                var targetTable = await _db.DiningTables.FirstOrDefaultAsync(
                    t => t.Id == toTableId && t.IsActive && !t.IsDeleted,
                    token);
                if (targetTable is null)
                {
                    return OperationResult<bool>.Failure("Bàn đích không tồn tại hoặc đã ngừng sử dụng.");
                }

                var targetOccupied = await _db.InvoiceTables.AnyAsync(
                    t => t.TableId == toTableId && t.ReleasedAt == null && !t.IsDeleted,
                    token);
                if (targetOccupied)
                {
                    return OperationResult<bool>.Failure("Bàn đích đang có khách.");
                }

                var now = DateTime.UtcNow;
                source.ReleasedAt = now;
                source.UpdatedAt = now;
                source.UpdatedBy = _currentUser.UserId;
                var target = new InvoiceTable
                {
                    InvoiceId = invoiceId,
                    TableId = toTableId,
                    AssignedAt = now,
                    IsPrimary = source.IsPrimary,
                    CreatedAt = now,
                    CreatedBy = _currentUser.UserId,
                };
                _db.InvoiceTables.Add(target);
                _db.TableTransferLogs.Add(new TableTransferLog
                {
                    InvoiceId = invoiceId,
                    FromTableId = fromTableId,
                    ToTableId = toTableId,
                    MovedAt = now,
                    MovedByEmployeeId = employeeId,
                    Note = cleanNote,
                    CreatedAt = now,
                    CreatedBy = _currentUser.UserId,
                });

                await _db.SaveChangesAsync(token);
                return OperationResult<bool>.Success(true);
            },
            IsolationLevel.Serializable,
            cancellationToken);
    }

    public async Task<OperationResult<bool>> MergeInvoicesAsync(
        int targetInvoiceId,
        int sourceInvoiceId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        if (targetInvoiceId == sourceInvoiceId)
        {
            return OperationResult<bool>.Failure("Không thể gộp hóa đơn với chính nó.");
        }

        return await ExecuteTransactionAsync(
            async token =>
            {

                var invoices = await _db.Invoices
                    .Where(i => (i.Id == targetInvoiceId || i.Id == sourceInvoiceId) &&
                                i.Status == 0 &&
                                !i.IsDeleted)
                    .OrderBy(i => i.Id)
                    .ToListAsync(token);
                if (invoices.Count != 2)
                {
                    return OperationResult<bool>.Failure("Cả hai hóa đơn phải đang ở trạng thái phục vụ.");
                }

                var target = invoices.Single(i => i.Id == targetInvoiceId);
                var source = invoices.Single(i => i.Id == sourceInvoiceId);

                var targetPrimaryTable = await _db.InvoiceTables
                    .Where(t => t.InvoiceId == targetInvoiceId && t.ReleasedAt == null && !t.IsDeleted)
                    .OrderByDescending(t => t.IsPrimary)
                    .ThenBy(t => t.Table.TableNumber)
                    .Select(t => t.TableId)
                    .FirstOrDefaultAsync(token);
                if (targetPrimaryTable == 0)
                {
                    return OperationResult<bool>.Failure("Hóa đơn nhận không có bàn đang phục vụ.");
                }

                var sourceTables = await _db.InvoiceTables
                    .Where(t => t.InvoiceId == sourceInvoiceId && t.ReleasedAt == null && !t.IsDeleted)
                    .ToListAsync(token);
                if (sourceTables.Count == 0)
                {
                    return OperationResult<bool>.Failure("Hóa đơn cần gộp không có bàn đang phục vụ.");
                }

                var targetTableIds = await _db.InvoiceTables
                    .Where(t => t.InvoiceId == targetInvoiceId && t.ReleasedAt == null && !t.IsDeleted)
                    .Select(t => t.TableId)
                    .ToListAsync(token);
                var allTableIds = targetTableIds.Concat(sourceTables.Select(t => t.TableId)).Distinct().ToList();
                var combinedCapacity = await _db.DiningTables
                    .Where(t => allTableIds.Contains(t.Id))
                    .SumAsync(t => t.MaxSeats, token);
                if (target.CustomerCount + source.CustomerCount > combinedCapacity)
                {
                    return OperationResult<bool>.Failure(
                        $"Sức chứa gộp là {combinedCapacity} chỗ, nhỏ hơn tổng {target.CustomerCount + source.CustomerCount} khách.");
                }

                var now = DateTime.UtcNow;
                foreach (var sourceTable in sourceTables)
                {
                    _db.TableTransferLogs.Add(new TableTransferLog
                    {
                        InvoiceId = targetInvoiceId,
                        FromTableId = sourceTable.TableId,
                        ToTableId = targetPrimaryTable,
                        MovedAt = now,
                        MovedByEmployeeId = employeeId,
                        Note = $"Gộp hóa đơn {source.InvoiceCode} vào {target.InvoiceCode}",
                        CreatedAt = now,
                        CreatedBy = _currentUser.UserId,
                    });
                    sourceTable.InvoiceId = targetInvoiceId;
                    sourceTable.IsPrimary = false;
                    sourceTable.UpdatedAt = now;
                    sourceTable.UpdatedBy = _currentUser.UserId;
                }

                var sourceDetails = await _db.InvoiceDetails
                    .Where(d => d.InvoiceId == sourceInvoiceId && !d.IsDeleted)
                    .ToListAsync(token);
                foreach (var detail in sourceDetails)
                {
                    detail.InvoiceId = targetInvoiceId;
                    detail.UpdatedAt = now;
                    detail.UpdatedBy = _currentUser.UserId;
                }

                source.Status = 2;
                source.ClosedAt = now;
                source.UpdatedAt = now;
                source.UpdatedBy = _currentUser.UserId;
                target.CustomerCount += source.CustomerCount;
                target.UpdatedAt = now;
                target.UpdatedBy = _currentUser.UserId;

                await _db.SaveChangesAsync(token);
                return OperationResult<bool>.Success(true);
            },
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private async Task<T> ExecuteTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private static string CreateInvoiceCode()
        => $"I{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
