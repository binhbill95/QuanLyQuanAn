using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public sealed class BankTransferSettingsViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn ngân hàng.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã BIN ngân hàng phải gồm 6 chữ số.")]
    [Display(Name = "Mã BIN ngân hàng")]
    public string BankCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng.")]
    [StringLength(100)]
    [Display(Name = "Tên ngân hàng")]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số tài khoản.")]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "Số tài khoản phải từ 5 đến 30 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "Số tài khoản chỉ được chứa chữ, số hoặc dấu gạch ngang.")]
    [Display(Name = "Số tài khoản")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản.")]
    [StringLength(25, MinimumLength = 2, ErrorMessage = "Tên chủ tài khoản tối đa 25 ký tự theo chuẩn VietQR.")]
    [Display(Name = "Tên chủ tài khoản")]
    public string AccountName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Chi nhánh (không bắt buộc)")]
    public string? Branch { get; set; }
}

public sealed class PaymentReceiptViewModel
{
    public required RestaurantManagement.Application.Models.Payments.PaymentReceiptDto Receipt { get; init; }

    public required RestaurantManagement.Application.Models.Payments.BankTransferSettingsDto Bank { get; init; }
}
