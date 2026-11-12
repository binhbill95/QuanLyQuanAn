using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Web.Models;

public sealed class SupportContactSettingsViewModel : IValidatableObject
{
    [StringLength(30, ErrorMessage = "Số hotline tối đa 30 ký tự.")]
    [RegularExpression(@"^\+?[0-9\s().-]+$", ErrorMessage = "Hotline chỉ gồm số, dấu +, dấu ngoặc, dấu chấm hoặc dấu gạch ngang.")]
    [Display(Name = "Số hotline")]
    public string? HotlineNumber { get; set; }

    [StringLength(255, ErrorMessage = "Đường dẫn Zalo tối đa 255 ký tự.")]
    [Display(Name = "Đường dẫn Zalo")]
    public string? ZaloUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(HotlineNumber))
        {
            var digitCount = HotlineNumber.Count(char.IsDigit);
            if (digitCount is < 7 or > 15)
            {
                yield return new ValidationResult(
                    "Hotline phải có từ 7 đến 15 chữ số.",
                    [nameof(HotlineNumber)]);
            }
        }

        if (!string.IsNullOrWhiteSpace(ZaloUrl) &&
            (!Uri.TryCreate(ZaloUrl, UriKind.Absolute, out var uri) ||
             uri.Scheme != Uri.UriSchemeHttps ||
             !string.Equals(uri.Host, "zalo.me", StringComparison.OrdinalIgnoreCase) ||
             uri.AbsolutePath == "/"))
        {
            yield return new ValidationResult(
                "Vui lòng nhập đường dẫn Zalo hợp lệ bắt đầu bằng https://zalo.me/.",
                [nameof(ZaloUrl)]);
        }
    }
}
