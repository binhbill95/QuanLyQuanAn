using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Menu;
using RestaurantManagement.Application.Models.Tables;

namespace RestaurantManagement.Web.Models;

public sealed class MenuIndexViewModel
{
    public IReadOnlyList<MenuItemListItemDto> Items { get; init; } = [];

    public IReadOnlyList<CategoryOptionDto> Categories { get; init; } = [];

    public string? Keyword { get; init; }

    public int? CategoryId { get; init; }

    public bool? IsAvailable { get; init; }

    public bool ShowDeleted { get; init; }

    public bool CanManage { get; init; }

    public bool CanToggleAvailability { get; init; }
}

public sealed class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Mã nhóm")]
    [Required(ErrorMessage = "Vui lòng nhập mã nhóm.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Mã nhóm phải từ {2} đến {1} ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9_]+$",
        ErrorMessage = "Mã nhóm chỉ gồm chữ cái, chữ số và dấu gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Tên nhóm")]
    [Required(ErrorMessage = "Vui lòng nhập tên nhóm.")]
    [StringLength(100, ErrorMessage = "Tên nhóm tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Thứ tự hiển thị")]
    [Range(0, 9999, ErrorMessage = "Thứ tự hiển thị phải từ 0 đến 9999.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;
}

public sealed class MenuItemFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Mã món")]
    [Required(ErrorMessage = "Vui lòng nhập mã món.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Mã món phải từ {2} đến {1} ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9_]+$",
        ErrorMessage = "Mã món chỉ gồm chữ cái, chữ số và dấu gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Tên món")]
    [Required(ErrorMessage = "Vui lòng nhập tên món.")]
    [StringLength(200, ErrorMessage = "Tên món tối đa 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Nhóm món")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhóm món.")]
    public int CategoryId { get; set; }

    [Display(Name = "Giá bán (đ)")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá bán phải từ 0 đến 999.999.999 đ.")]
    public decimal Price { get; set; }

    [Display(Name = "Đơn vị tính")]
    [StringLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    public string? Unit { get; set; }

    [Display(Name = "Mô tả")]
    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    [Display(Name = "Đường dẫn ảnh")]
    [StringLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự.")]
    [RegularExpression(
        "^(https?://|/).*$",
        ErrorMessage = "Ảnh phải là URL http(s) hoặc đường dẫn bắt đầu bằng '/'.")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Còn món")]
    public bool IsAvailable { get; set; } = true;

    public IReadOnlyList<CategoryOptionDto> Categories { get; set; } = [];
}

public sealed class DiningTablesIndexViewModel
{
    public IReadOnlyList<DiningTableDto> Tables { get; init; } = [];

    public bool CanManage { get; init; }
}

public sealed class DiningTableFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Số bàn")]
    [Required(ErrorMessage = "Vui lòng nhập số bàn.")]
    [StringLength(10, ErrorMessage = "Số bàn tối đa 10 ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9_-]+$",
        ErrorMessage = "Số bàn chỉ gồm chữ cái, chữ số, dấu gạch dưới hoặc gạch ngang.")]
    public string TableNumber { get; set; } = string.Empty;

    [Display(Name = "Khu vực")]
    public int? AreaId { get; set; }

    [Display(Name = "Số chỗ tối đa")]
    [Range(1, 100, ErrorMessage = "Số chỗ ngồi phải từ 1 đến 100.")]
    public int MaxSeats { get; set; } = 4;

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<AreaOptionDto> Areas { get; set; } = [];
}

public sealed class AreaFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Tên khu vực")]
    [Required(ErrorMessage = "Vui lòng nhập tên khu vực.")]
    [StringLength(50, ErrorMessage = "Tên khu vực tối đa 50 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Thứ tự hiển thị")]
    [Range(0, 9999, ErrorMessage = "Thứ tự hiển thị phải từ 0 đến 9999.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; set; } = true;
}
