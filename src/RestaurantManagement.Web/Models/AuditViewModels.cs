using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Application.Models.Audit;

namespace RestaurantManagement.Web.Models;

public sealed class AuditFilterViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Từ ngày")]
    public DateOnly? FromDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Đến ngày")]
    public DateOnly? ToDate { get; set; }

    [Display(Name = "Thao tác")]
    public string? Operation { get; set; }

    [StringLength(100)]
    [Display(Name = "Từ khóa")]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
}

public sealed class AuditIndexViewModel
{
    public required AuditActivityFilter Filter { get; init; }

    public required AuditActivityPage Results { get; init; }

    public int PageCount => Math.Max(
        1,
        (int)Math.Ceiling(Results.TotalCount / (double)Results.PageSize));
}
