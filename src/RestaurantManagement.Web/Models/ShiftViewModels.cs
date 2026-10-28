using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Attendance;
using RestaurantManagement.Application.Models.Schedules;
using RestaurantManagement.Application.Models.Shifts;

namespace RestaurantManagement.Web.Models;

public sealed class ShiftFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Tên ca")]
    [Required(ErrorMessage = "Vui lòng nhập tên ca.")]
    [StringLength(50, ErrorMessage = "Tên ca tối đa 50 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Giờ bắt đầu")]
    [DataType(DataType.Time)]
    [DisplayFormat(DataFormatString = "{0:HH\\:mm}", ApplyFormatInEditMode = true)]
    public TimeOnly StartTime { get; set; } = new(8, 0);

    [Display(Name = "Giờ kết thúc")]
    [DataType(DataType.Time)]
    [DisplayFormat(DataFormatString = "{0:HH\\:mm}", ApplyFormatInEditMode = true)]
    public TimeOnly EndTime { get; set; } = new(16, 0);

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;
}

public sealed class ShiftCheckboxItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public bool Selected { get; init; }
}

public sealed class AssignShiftsViewModel
{
    public int EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public IReadOnlyList<ShiftCheckboxItem> Shifts { get; init; } = [];
}

public sealed class ScheduleIndexViewModel
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public int? EmployeeId { get; init; }

    public IReadOnlyList<ScheduleListItemDto> Schedules { get; init; } = [];

    public IReadOnlyList<EmployeeLookupDto> Employees { get; init; } = [];

    public IReadOnlyList<ShiftDto> Shifts { get; init; } = [];
}

public sealed class AttendanceIndexViewModel
{
    public DateOnly Date { get; init; }

    public int? EmployeeId { get; init; }

    public IReadOnlyList<AttendanceRowDto> Rows { get; init; } = [];

    public IReadOnlyList<EmployeeLookupDto> Employees { get; init; } = [];

    public bool IsManagement { get; init; }

    public bool HasEmployeeProfile { get; init; }

    public int? CurrentEmployeeId { get; init; }

    public string? Notice { get; init; }
}
