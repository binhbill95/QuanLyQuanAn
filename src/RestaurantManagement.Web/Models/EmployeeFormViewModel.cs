using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestaurantManagement.Application.Common;
using RestaurantManagement.Application.Models.Employees;

namespace RestaurantManagement.Web.Models;

public sealed class EmployeeIndexViewModel
{
    public required PagedResult<EmployeeListItemDto> Employees { get; init; }

    public string? Keyword { get; init; }

    public int? PositionId { get; init; }

    public byte? Status { get; init; }

    public IReadOnlyList<PositionLookupDto> Positions { get; init; } = [];
}

/// <summary>Form Thêm/Sửa nhân viên (dùng chung một view model).</summary>
public sealed class EmployeeFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Mã nhân viên")]
    [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
    [StringLength(20, ErrorMessage = "Mã nhân viên tối đa 20 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9_-]{2,20}$",
        ErrorMessage = "Mã nhân viên chỉ gồm chữ, số, gạch ngang hoặc gạch dưới.")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Giới tính")]
    [Range(0, 2, ErrorMessage = "Giới tính không hợp lệ.")]
    public byte? Gender { get; set; }

    [Display(Name = "Ngày sinh")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Vị trí làm việc")]
    [Required(ErrorMessage = "Vui lòng chọn vị trí.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vị trí.")]
    public int PositionId { get; set; }

    [Display(Name = "Điện thoại")]
    [StringLength(20, ErrorMessage = "Điện thoại tối đa 20 ký tự.")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0901234567).")]
    public string? Phone { get; set; }

    [Display(Name = "Địa chỉ")]
    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }

    [Display(Name = "Số CCCD/CMND")]
    [Required(ErrorMessage = "Vui lòng nhập số CCCD/CMND.")]
    [StringLength(20, ErrorMessage = "Số CCCD/CMND tối đa 20 ký tự.")]
    [RegularExpression(@"^\d{9,12}$", ErrorMessage = "Số CCCD/CMND phải gồm 9–12 chữ số.")]
    public string IdentityNumber { get; set; } = string.Empty;

    [Display(Name = "Ngày vào làm")]
    [Required(ErrorMessage = "Vui lòng chọn ngày vào làm.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Ngày nghỉ")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Đơn giá ca (VNĐ)")]
    [Range(0, 1_000_000_000, ErrorMessage = "Đơn giá ca không hợp lệ.")]
    public decimal? ShiftRate { get; set; }

    [Display(Name = "Trạng thái")]
    [Range(0, 2, ErrorMessage = "Trạng thái không hợp lệ.")]
    public byte Status { get; set; }

    [Display(Name = "Ghi chú")]
    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> PositionOptions { get; set; } = [];

    public static IReadOnlyList<SelectListItem> GenderOptions { get; } =
    [
        new SelectListItem("Nữ", "0"),
        new SelectListItem("Nam", "1"),
        new SelectListItem("Khác", "2"),
    ];

    public static IReadOnlyList<SelectListItem> StatusOptions { get; } =
    [
        new SelectListItem("Đang làm", "0"),
        new SelectListItem("Tạm nghỉ", "1"),
        new SelectListItem("Đã nghỉ", "2"),
    ];
}
