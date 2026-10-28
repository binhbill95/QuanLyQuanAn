using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Tables;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Tables;

public sealed class TableManagementService : ITableManagementService
{
    private static readonly Regex TableNumberPattern = new(
        "^[A-Za-z0-9_-]{1,10}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TableManagementService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AreaDto>> GetAreasAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Areas
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .Select(a => new AreaDto(
                a.Id,
                a.Name,
                a.DisplayOrder,
                a.IsActive,
                a.DiningTables.Count(t => !t.IsDeleted)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AreaOptionDto>> GetAreaOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Areas
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .Select(a => new AreaOptionDto(a.Id, a.Name, a.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<AreaInput?> GetAreaInputAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Areas
            .AsNoTracking()
            .Where(a => a.Id == id && !a.IsDeleted)
            .Select(a => new AreaInput(a.Name, a.DisplayOrder, a.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAreaAsync(AreaInput input, CancellationToken cancellationToken = default)
    {
        var error = await ValidateAreaAsync(input, null, cancellationToken);
        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var now = DateTime.UtcNow;
        var area = new Area
        {
            Name = input.Name.Trim(),
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
        };

        _db.Areas.Add(area);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(area.Id);
    }

    public async Task<OperationResult<bool>> UpdateAreaAsync(
        int id,
        AreaInput input,
        CancellationToken cancellationToken = default)
    {
        var area = await _db.Areas.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        if (area is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy khu vực.");
        }

        var error = await ValidateAreaAsync(input, id, cancellationToken);
        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        if (!input.IsActive && await _db.DiningTables.AnyAsync(
                t => t.AreaId == id && !t.IsDeleted && t.IsActive, cancellationToken))
        {
            return OperationResult<bool>.Failure("Hãy chuyển hoặc ngừng sử dụng các bàn trong khu vực trước.");
        }

        area.Name = input.Name.Trim();
        area.DisplayOrder = input.DisplayOrder;
        area.IsActive = input.IsActive;
        area.UpdatedAt = DateTime.UtcNow;
        area.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteAreaAsync(int id, CancellationToken cancellationToken = default)
    {
        var area = await _db.Areas.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        if (area is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy khu vực.");
        }

        if (await _db.DiningTables.AnyAsync(t => t.AreaId == id && !t.IsDeleted, cancellationToken))
        {
            return OperationResult<bool>.Failure("Khu vực vẫn còn bàn. Hãy chuyển hoặc xóa mềm các bàn trước.");
        }

        area.IsDeleted = true;
        area.DeletedAt = DateTime.UtcNow;
        area.UpdatedAt = DateTime.UtcNow;
        area.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<DiningTableDto>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        var statusRows = await _db.VwDiningTableStatuses
            .AsNoTracking()
            .ToDictionaryAsync(s => s.TableId, s => s.StatusName, cancellationToken);

        var tables = await _db.DiningTables
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Area == null ? int.MaxValue : t.Area.DisplayOrder)
            .ThenBy(t => t.Area == null ? string.Empty : t.Area.Name)
            .ThenBy(t => t.TableNumber)
            .Select(t => new
            {
                t.Id,
                t.TableNumber,
                t.AreaId,
                AreaName = t.Area == null || t.Area.IsDeleted ? null : t.Area.Name,
                t.MaxSeats,
                t.IsActive,
            })
            .ToListAsync(cancellationToken);

        return tables.Select(t => new DiningTableDto(
                t.Id,
                t.TableNumber,
                t.AreaId,
                t.AreaName,
                t.MaxSeats,
                t.IsActive,
                t.IsActive && statusRows.TryGetValue(t.Id, out var status) ? status : "Ngừng sử dụng"))
            .ToList();
    }

    public async Task<DiningTableInput?> GetTableInputAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.DiningTables
            .AsNoTracking()
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new DiningTableInput(t.TableNumber, t.AreaId, t.MaxSeats, t.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateTableAsync(
        DiningTableInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateTableAsync(input, null, cancellationToken);
        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var table = new DiningTable
        {
            TableNumber = input.TableNumber.Trim().ToUpperInvariant(),
            AreaId = input.AreaId,
            MaxSeats = input.MaxSeats,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        _db.DiningTables.Add(table);
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(table.Id);
    }

    public async Task<OperationResult<bool>> UpdateTableAsync(
        int id,
        DiningTableInput input,
        CancellationToken cancellationToken = default)
    {
        var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        if (table is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy bàn.");
        }

        var error = await ValidateTableAsync(input, id, cancellationToken);
        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        var isAssigned = await IsAssignedAsync(id, cancellationToken);
        if (isAssigned &&
            (!input.IsActive ||
             !string.Equals(table.TableNumber, input.TableNumber.Trim(), StringComparison.OrdinalIgnoreCase) ||
             table.AreaId != input.AreaId ||
             table.MaxSeats != input.MaxSeats))
        {
            return OperationResult<bool>.Failure("Không thể thay đổi thông tin hoặc ngừng sử dụng bàn đang có khách.");
        }

        table.TableNumber = input.TableNumber.Trim().ToUpperInvariant();
        table.AreaId = input.AreaId;
        table.MaxSeats = input.MaxSeats;
        table.IsActive = input.IsActive;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> SetTableActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        if (table is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy bàn.");
        }

        if (!isActive && table.IsActive && await IsAssignedAsync(id, cancellationToken))
        {
            return OperationResult<bool>.Failure("Không thể ngừng sử dụng bàn đang có khách.");
        }

        if (table.IsActive == isActive)
        {
            return OperationResult<bool>.Success(true);
        }

        table.IsActive = isActive;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteTableAsync(int id, CancellationToken cancellationToken = default)
    {
        var table = await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        if (table is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy bàn.");
        }

        if (await IsAssignedAsync(id, cancellationToken))
        {
            return OperationResult<bool>.Failure("Không thể xóa mềm bàn đang có khách.");
        }

        table.IsDeleted = true;
        table.DeletedAt = DateTime.UtcNow;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private async Task<string?> ValidateAreaAsync(
        AreaInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var name = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return "Tên khu vực không được để trống và tối đa 50 ký tự.";
        }

        if (input.DisplayOrder < 0)
        {
            return "Thứ tự hiển thị không được âm.";
        }

        if (await _db.Areas.AnyAsync(
                a => a.Name == name && (excludeId == null || a.Id != excludeId), cancellationToken))
        {
            return $"Tên khu vực '{name}' đã tồn tại.";
        }

        return null;
    }

    private async Task<string?> ValidateTableAsync(
        DiningTableInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var number = input.TableNumber.Trim();
        if (!TableNumberPattern.IsMatch(number))
        {
            return "Số bàn chỉ gồm chữ cái, chữ số, dấu gạch dưới hoặc gạch ngang, tối đa 10 ký tự.";
        }

        if (input.MaxSeats < 1 || input.MaxSeats > 100)
        {
            return "Số chỗ ngồi phải từ 1 đến 100.";
        }

        if (await _db.DiningTables.AnyAsync(
                t => t.TableNumber == number.ToUpper() && (excludeId == null || t.Id != excludeId),
                cancellationToken))
        {
            return $"Số bàn '{number}' đã tồn tại.";
        }

        if (input.AreaId.HasValue && !await _db.Areas.AnyAsync(
                a => a.Id == input.AreaId.Value && !a.IsDeleted && a.IsActive, cancellationToken))
        {
            return "Khu vực không tồn tại hoặc đã ngừng sử dụng.";
        }

        return null;
    }

    private Task<bool> IsAssignedAsync(int tableId, CancellationToken cancellationToken)
    {
        return _db.InvoiceTables.AnyAsync(
            assignment => assignment.TableId == tableId &&
                          assignment.ReleasedAt == null &&
                          !assignment.IsDeleted,
            cancellationToken);
    }
}
