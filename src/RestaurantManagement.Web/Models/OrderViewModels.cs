using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Orders;

namespace RestaurantManagement.Web.Models;

public sealed class OrderIndexViewModel
{
    public IReadOnlyList<ActiveTableOrderDto> Tables { get; init; } = [];

    public IReadOnlyList<OrderMenuOptionDto> MenuItems { get; init; } = [];

    public IReadOnlyList<ActiveInvoiceDto> ActiveInvoices { get; init; } = [];

    public InvoiceOrderDto? Invoice { get; init; }

    public int? SelectedTableId { get; init; }

    public bool CanManageTables { get; init; }

    public OpenInvoiceFormViewModel OpenInvoice { get; init; } = new();
}

public sealed class OpenInvoiceFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn bàn.")]
    public int TableId { get; set; }

    [Display(Name = "Số lượng khách")]
    [Range(1, 100, ErrorMessage = "Số lượng khách phải từ 1 đến 100.")]
    public int CustomerCount { get; set; } = 1;
}

public sealed class AddOrderItemFormViewModel
{
    public int InvoiceId { get; set; }

    [Display(Name = "Món")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn món.")]
    public int MenuItemId { get; set; }

    [Display(Name = "Số lượng")]
    [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Ghi chú")]
    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }
}

public sealed class CancelOrderItemFormViewModel
{
    public int InvoiceId { get; set; }

    public int DetailId { get; set; }

    [Display(Name = "Lý do hủy")]
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy món.")]
    [StringLength(255, ErrorMessage = "Lý do hủy tối đa 255 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class ReplaceOrderItemFormViewModel
{
    public int InvoiceId { get; set; }

    public int DetailId { get; set; }

    [Display(Name = "Món thay thế")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn món thay thế.")]
    public int MenuItemId { get; set; }

    [Display(Name = "Số lượng")]
    [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Ghi chú món")]
    [StringLength(255, ErrorMessage = "Ghi chú món tối đa 255 ký tự.")]
    public string? Note { get; set; }

    [Display(Name = "Lý do đổi")]
    [Required(ErrorMessage = "Vui lòng nhập lý do đổi món.")]
    [StringLength(255, ErrorMessage = "Lý do đổi tối đa 255 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
