using System.Security.Claims;
using RestaurantManagement.Application.Abstractions;

namespace RestaurantManagement.Web.Services;

/// <summary>
/// Đọc thông tin người dùng đang đăng nhập từ ClaimsPrincipal (cookie authentication).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public string? RoleCode => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public int? EmployeeId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue("employee_id");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
