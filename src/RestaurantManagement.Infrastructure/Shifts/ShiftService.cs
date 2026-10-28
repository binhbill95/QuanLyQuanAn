using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Shifts;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Shifts;

public sealed class ShiftService : IShiftService
{
    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ShiftService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ShiftDto>> GetAllAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Shifts
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .Select(s => new ShiftDto(
                s.Id,
                s.Name,
                s.StartTime,
                s.EndTime,
                s.IsActive,
                s.EmployeeShifts.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShiftInput?> GetInputAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Shifts
            .AsNoTracking()
            .Where(s => s.Id == id && !s.IsDeleted)
            .Select(s => new ShiftInput(s.Name, s.StartTime, s.EndTime, s.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAsync(
        ShiftInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);

        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var shift = new Shift
        {
            Name = input.Name.Trim(),
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(shift.Id);
    }

    public async Task<OperationResult<bool>> UpdateAsync(
        int id,
        ShiftInput input,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (shift is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy ca làm.");
        }

        var error = await ValidateAsync(input, id, cancellationToken);

        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        shift.Name = input.Name.Trim();
        shift.StartTime = input.StartTime;
        shift.EndTime = input.EndTime;
        shift.IsActive = input.IsActive;
        shift.UpdatedAt = DateTime.UtcNow;
        shift.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (shift is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy ca làm.");
        }

        shift.IsActive = isActive;
        shift.UpdatedAt = DateTime.UtcNow;
        shift.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<EmployeeShiftAssignmentDto>> GetAssignmentsAsync(
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == 0);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(e => e.EmployeeCode.Contains(value) || e.FullName.Contains(value));
        }

        var rows = await query
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new
            {
                e.Id,
                e.EmployeeCode,
                e.FullName,
                PositionName = e.Position.Name,
                Shifts = e.EmployeeShifts
                    .Select(es => new { es.ShiftId, es.Shift.Name })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new EmployeeShiftAssignmentDto(
                r.Id,
                r.EmployeeCode,
                r.FullName,
                r.PositionName,
                r.Shifts.Select(s => s.ShiftId).ToList(),
                r.Shifts.Select(s => s.Name).ToList()))
            .ToList();
    }

    public async Task<OperationResult<bool>> SetEmployeeShiftsAsync(
        int employeeId,
        IReadOnlyList<int> shiftIds,
        CancellationToken cancellationToken = default)
    {
        var employeeExists = await _db.Employees
            .AnyAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);

        if (!employeeExists)
        {
            return OperationResult<bool>.Failure("Không tìm thấy nhân viên.");
        }

        var distinctIds = shiftIds.Distinct().ToList();

        var validIds = await _db.Shifts
            .Where(s => distinctIds.Contains(s.Id) && !s.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (validIds.Count != distinctIds.Count)
        {
            return OperationResult<bool>.Failure("Có ca làm không hợp lệ.");
        }

        var existing = await _db.EmployeeShifts
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var toRemove = existing
            .Where(es => !distinctIds.Contains(es.ShiftId))
            .ToList();

        if (toRemove.Count > 0)
        {
            _db.EmployeeShifts.RemoveRange(toRemove);
        }

        var toAdd = distinctIds
            .Where(id => existing.All(es => es.ShiftId != id))
            .Select(id => new EmployeeShift
            {
                EmployeeId = employeeId,
                ShiftId = id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
            });

        _db.EmployeeShifts.AddRange(toAdd);

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    private async Task<string?> ValidateAsync(
        ShiftInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var name = input.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Tên ca không được để trống.";
        }

        if (input.StartTime >= input.EndTime)
        {
            return "Giờ bắt đầu phải trước giờ kết thúc.";
        }

        // UQ_Shift_Name áp dụng cả bản ghi xóa mềm.
        var nameExists = await _db.Shifts
            .AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId), cancellationToken);

        if (nameExists)
        {
            return $"Tên ca '{name}' đã tồn tại.";
        }

        return null;
    }
}
