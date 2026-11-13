using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Branches;

namespace RestaurantManagement.Web.Models;

public sealed class BranchFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Mã chi nhánh")]
    [Required(ErrorMessage = "Vui lòng nhập mã chi nhánh.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Mã chi nhánh phải từ {2} đến {1} ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9_-]+$",
        ErrorMessage = "Mã chi nhánh chỉ gồm chữ cái, chữ số, gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Tên chi nhánh")]
    [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh.")]
    [StringLength(100, ErrorMessage = "Tên chi nhánh tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ")]
    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }

    [Display(Name = "Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string? Phone { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public static BranchFormViewModel From(BranchInput input, int? id = null) => new()
    {
        Id = id,
        Code = input.Code,
        Name = input.Name,
        Address = input.Address,
        Phone = input.Phone,
        IsActive = input.IsActive,
    };

    public BranchInput ToInput() => new(Code, Name, Address, Phone, IsActive);
}
