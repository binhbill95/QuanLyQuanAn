using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Employees;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Employees;

/// <summary>
/// Nghiệp vụ hồ sơ nhân viên.
/// Quy tắc nghiệp vụ được kiểm tra ở đây (không chỉ dựa vào CHECK constraint của DB)
/// để trả về thông báo tiếng Việt cho người dùng.
/// </summary>
public sealed class EmployeeService : IEmployeeService
{
    private const byte StatusResigned = 2;

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EmployeeService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EmployeeListItemDto>> SearchAsync(
        EmployeeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = _db.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();

            query = query.Where(e =>
                e.EmployeeCode.Contains(keyword) ||
                e.FullName.Contains(keyword) ||
                (e.Phone != null && e.Phone.Contains(keyword)));
        }

        if (request.PositionId.HasValue)
        {
            query = query.Where(e => e.PositionId == request.PositionId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListItemDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Position.Name,
                e.Phone,
                e.Status,
                e.StartDate,
                e.AppUser != null))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<EmployeeDetailsDto?> GetDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == id && !e.IsDeleted)
            .Select(e => new EmployeeDetailsDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Gender,
                e.DateOfBirth,
                e.PositionId,
                e.Position.Name,
                e.Phone,
                e.Address,
                e.IdentityNumber,
                e.StartDate,
                e.EndDate,
                e.ShiftRate,
                e.Status,
                e.Note,
                e.EmployeeShifts.Select(es => es.Shift.Name).ToList(),
                e.AppUser != null ? e.AppUser.Username : null,
                e.CreatedAt,
                e.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmployeeInput?> GetInputAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == id && !e.IsDeleted)
            .Select(e => new EmployeeInput(
                e.EmployeeCode,
                e.FullName,
                e.Gender,
                e.DateOfBirth,
                e.PositionId,
                e.Phone,
                e.Address,
                e.IdentityNumber,
                e.StartDate,
                e.EndDate,
                e.ShiftRate,
                e.Status,
                e.Note))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> CreateAsync(
        EmployeeInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);

        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        var employee = new Employee
        {
            EmployeeCode = input.EmployeeCode.Trim(),
            FullName = input.FullName.Trim(),
            Gender = input.Gender,
            DateOfBirth = input.DateOfBirth,
            PositionId = input.PositionId,
            Phone = Clean(input.Phone),
            Address = Clean(input.Address),
            IdentityNumber = input.IdentityNumber.Trim(),
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            ShiftRate = input.ShiftRate,
            Status = input.Status,
            Note = Clean(input.Note),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(employee.Id);
    }

    public async Task<OperationResult<bool>> UpdateAsync(
        int id,
        EmployeeInput input,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .Include(e => e.AppUser)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (employee is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy nhân viên.");
        }

        var error = await ValidateAsync(input, id, cancellationToken);

        if (error is not null)
        {
            return OperationResult<bool>.Failure(error);
        }

        employee.EmployeeCode = input.EmployeeCode.Trim();
        employee.FullName = input.FullName.Trim();
        employee.Gender = input.Gender;
        employee.DateOfBirth = input.DateOfBirth;
        employee.PositionId = input.PositionId;
        employee.Phone = Clean(input.Phone);
        employee.Address = Clean(input.Address);
        employee.IdentityNumber = input.IdentityNumber.Trim();
        employee.StartDate = input.StartDate;
        employee.EndDate = input.EndDate;
        employee.ShiftRate = input.ShiftRate;
        employee.Status = input.Status;
        employee.Note = Clean(input.Note);
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUser.UserId;

        // Nhân viên đã nghỉ thì khóa luôn tài khoản đăng nhập (nếu có).
        if (input.Status == StatusResigned && employee.AppUser is { IsActive: true })
        {
            employee.AppUser.IsActive = false;
            employee.AppUser.UpdatedAt = DateTime.UtcNow;
            employee.AppUser.UpdatedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> SoftDeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .Include(e => e.AppUser)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (employee is null)
        {
            return OperationResult<bool>.Failure("Không tìm thấy nhân viên.");
        }

        if (employee.AppUser is not null && employee.AppUser.Id == _currentUser.UserId)
        {
            return OperationResult<bool>.Failure("Không thể xóa nhân viên đang đăng nhập.");
        }

        employee.IsDeleted = true;
        employee.DeletedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = _currentUser.UserId;

        if (employee.AppUser is { IsActive: true })
        {
            employee.AppUser.IsActive = false;
            employee.AppUser.UpdatedAt = DateTime.UtcNow;
            employee.AppUser.UpdatedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<PositionLookupDto>> GetPositionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Positions
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new PositionLookupDto(p.Id, p.Code, p.Name, p.DefaultShiftRate, p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> SuggestEmployeeCodeAsync(CancellationToken cancellationToken = default)
    {
        var codes = await _db.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeCode.StartsWith("NV"))
            .Select(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        var max = codes
            .Select(code => int.TryParse(code[2..], out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"NV{max + 1:D3}";
    }

    private async Task<string?> ValidateAsync(
        EmployeeInput input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            return "Mã nhân viên không được để trống.";
        }

        if (string.IsNullOrWhiteSpace(input.FullName))
        {
            return "Họ tên không được để trống.";
        }

        if (string.IsNullOrWhiteSpace(input.IdentityNumber))
        {
            return "Số CCCD/CMND không được để trống.";
        }

        if (input.Gender is > 2)
        {
            return "Giới tính không hợp lệ.";
        }

        if (input.Status is > 2)
        {
            return "Trạng thái nhân viên không hợp lệ.";
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (input.DateOfBirth.HasValue && (input.DateOfBirth.Value > today || input.DateOfBirth.Value.Year < 1900))
        {
            return "Ngày sinh không hợp lệ.";
        }

        if (input.EndDate.HasValue && input.EndDate.Value < input.StartDate)
        {
            return "Ngày nghỉ phải sau hoặc bằng ngày vào làm.";
        }

        if (input.Status == StatusResigned && input.EndDate is null)
        {
            return "Nhân viên đã nghỉ phải có ngày nghỉ.";
        }

        if (input.ShiftRate is < 0)
        {
            return "Đơn giá ca không được âm.";
        }

        var positionExists = await _db.Positions
            .AnyAsync(p => p.Id == input.PositionId && !p.IsDeleted, cancellationToken);

        if (!positionExists)
        {
            return "Vị trí làm việc không hợp lệ.";
        }

        var code = input.EmployeeCode.Trim();

        // Unique index của DB áp dụng cả bản ghi đã xóa mềm → kiểm tra toàn bộ bảng.
        var codeExists = await _db.Employees
            .AnyAsync(e => e.EmployeeCode == code && (excludeId == null || e.Id != excludeId), cancellationToken);

        if (codeExists)
        {
            return $"Mã nhân viên '{code}' đã tồn tại.";
        }

        var identity = input.IdentityNumber.Trim();

        var identityExists = await _db.Employees
            .AnyAsync(e => e.IdentityNumber == identity && (excludeId == null || e.Id != excludeId), cancellationToken);

        if (identityExists)
        {
            return $"Số CCCD/CMND '{identity}' đã được dùng cho nhân viên khác.";
        }

        return null;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
