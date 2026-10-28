using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Accounts;

namespace RestaurantManagement.Web.Models;

public sealed class AccountIndexViewModel
{
    public IReadOnlyList<AccountListItemDto> Accounts { get; init; } = [];

    public IReadOnlyList<AccountRoleOptionDto> Roles { get; init; } = [];

    public string? Keyword { get; init; }

    public int? RoleId { get; init; }

    public bool? IsActive { get; init; }

    public int CurrentUserId { get; init; }
}

public sealed class AccountCreateViewModel
{
    [Display(Name = "Nhân viên")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhân viên.")]
    public int EmployeeId { get; set; }

    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ {2} đến {1} ký tự.")]
    [RegularExpression(
        "^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Tên đăng nhập chỉ gồm chữ cái, chữ số và các ký tự . _ -")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vai trò.")]
    public int RoleId { get; set; }

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Nhập lại mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IReadOnlyList<AccountEmployeeOptionDto> Employees { get; set; } = [];

    public IReadOnlyList<AccountRoleOptionDto> Roles { get; set; } = [];
}

public sealed class AccountEditViewModel
{
    public int UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string? EmployeeCode { get; init; }

    public string? EmployeeName { get; init; }

    public string RoleCode { get; init; } = string.Empty;

    [Display(Name = "Vai trò")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vai trò.")]
    public int RoleId { get; set; }

    public IReadOnlyList<AccountRoleOptionDto> Roles { get; set; } = [];
}

public sealed class AccountResetPasswordViewModel
{
    public int UserId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string? EmployeeName { get; init; }

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Nhập lại mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
