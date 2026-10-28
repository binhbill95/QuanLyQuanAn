using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Orders;
using RestaurantManagement.Application.Models.Payments;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PaymentService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PaymentCheckoutDto?> GetCheckoutAsync(
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
                Subtotal = i.InvoiceDetails
                    .Where(d => !d.IsDeleted && d.Status != 3)
                    .Sum(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0m,
                UnfinishedItemCount = i.InvoiceDetails.Count(
                    d => !d.IsDeleted && (d.Status == 0 || d.Status == 1)),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return invoice is null
            ? null
            : new PaymentCheckoutDto(
                invoice.Id,
                invoice.InvoiceCode,
                invoice.CustomerCount,
                invoice.OpenedAt,
                invoice.Tables,
                invoice.Items,
                invoice.Subtotal,
                invoice.UnfinishedItemCount);
    }

    public async Task<PaymentReceiptDto?> GetReceiptAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.InvoiceId == invoiceId &&
                        !p.IsDeleted &&
                        p.Invoice.Status == 1 &&
                        !p.Invoice.IsDeleted)
            .Select(p => new
            {
                p.Invoice.Id,
                p.Invoice.InvoiceCode,
                p.Invoice.CustomerCount,
                p.Invoice.OpenedAt,
                p.Invoice.DiscountAmount,
                p.Amount,
                p.Method,
                p.PaidAt,
                PaidByEmployeeName = p.PaidByEmployee.FullName,
                p.Note,
                Tables = p.Invoice.InvoiceTables
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.IsPrimary)
                    .ThenBy(t => t.Table.TableNumber)
                    .Select(t => new OrderTableDto(t.TableId, t.Table.TableNumber, t.IsPrimary))
                    .ToList(),
                Items = p.Invoice.InvoiceDetails
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
                Subtotal = p.Invoice.InvoiceDetails
                    .Where(d => !d.IsDeleted && d.Status != 3)
                    .Sum(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return payment is null
            ? null
            : new PaymentReceiptDto(
                payment.Id,
                payment.InvoiceCode,
                payment.CustomerCount,
                payment.OpenedAt,
                payment.Tables,
                payment.Items,
                payment.Subtotal,
                payment.DiscountAmount,
                payment.Amount,
                payment.Method,
                payment.PaidAt,
                payment.PaidByEmployeeName,
                payment.Note);
    }

    public async Task<OperationResult<int>> PayAsync(
        PayInvoiceInput input,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        if (input.Method is > 1)
        {
            return OperationResult<int>.Failure("Phương thức thanh toán không hợp lệ.");
        }

        if (input.DiscountAmount < 0 ||
            decimal.Round(input.DiscountAmount, 2) != input.DiscountAmount)
        {
            return OperationResult<int>.Failure("Giảm giá phải không âm và tối đa 2 chữ số thập phân.");
        }

        if (input.DiscountAmount > 0 &&
            !string.Equals(_currentUser.RoleCode, "MANAGER", StringComparison.Ordinal))
        {
            return OperationResult<int>.Failure("Chỉ quản lý được quyền áp dụng giảm giá.");
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

                var invoice = await _db.Invoices.FirstOrDefaultAsync(
                    i => i.Id == input.InvoiceId && i.Status == 0 && !i.IsDeleted,
                    token);
                if (invoice is null)
                {
                    return OperationResult<int>.Failure("Hóa đơn không tồn tại hoặc đã được thanh toán.");
                }

                if (await _db.Payments.AnyAsync(
                        p => p.InvoiceId == invoice.Id && !p.IsDeleted,
                        token))
                {
                    return OperationResult<int>.Failure("Hóa đơn đã có giao dịch thanh toán.");
                }

                var details = await _db.InvoiceDetails
                    .Where(d => d.InvoiceId == invoice.Id && !d.IsDeleted)
                    .ToListAsync(token);
                var unfinishedItemCount = details.Count(d => d.Status is 0 or 1);
                if (unfinishedItemCount > 0 && !input.ConfirmUnfinishedItems)
                {
                    return OperationResult<int>.Failure(
                        "Hóa đơn còn món chưa hoàn thành. Vui lòng xác nhận vẫn tiếp tục thanh toán.");
                }

                var subtotal = details
                    .Where(d => d.Status != 3)
                    .Sum(d => d.Quantity * d.UnitPrice);
                if (subtotal <= 0)
                {
                    return OperationResult<int>.Failure("Hóa đơn chưa có món hợp lệ để thanh toán.");
                }

                if (input.DiscountAmount > subtotal)
                {
                    return OperationResult<int>.Failure("Giảm giá không được lớn hơn tiền món.");
                }

                var activeTables = await _db.InvoiceTables
                    .Where(t => t.InvoiceId == invoice.Id && t.ReleasedAt == null && !t.IsDeleted)
                    .ToListAsync(token);
                if (activeTables.Count == 0)
                {
                    return OperationResult<int>.Failure("Hóa đơn không còn bàn đang phục vụ.");
                }

                var paidAt = DateTime.UtcNow;
                var userId = _currentUser.UserId;
                invoice.DiscountAmount = input.DiscountAmount;
                invoice.Status = 1;
                invoice.ClosedAt = paidAt;
                invoice.UpdatedAt = paidAt;
                invoice.UpdatedBy = userId;

                foreach (var table in activeTables)
                {
                    table.ReleasedAt = paidAt;
                    table.UpdatedAt = paidAt;
                    table.UpdatedBy = userId;
                }

                _db.Payments.Add(new Payment
                {
                    InvoiceId = invoice.Id,
                    Method = input.Method,
                    Amount = subtotal - input.DiscountAmount,
                    PaidAt = paidAt,
                    PaidByEmployeeId = employeeId,
                    Note = Clean(input.Note),
                    CreatedAt = paidAt,
                    CreatedBy = userId,
                });

                await _db.SaveChangesAsync(token);
                return OperationResult<int>.Success(invoice.Id);
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

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
