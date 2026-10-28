using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Infrastructure;
using RestaurantManagement.Infrastructure.Persistence;
using RestaurantManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + tầng Infrastructure (DbContext, DI)
builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------------------------------------------------------------------------
// Xác thực: cookie-based, dữ liệu người dùng lấy từ bảng AppUser/Role.
// ---------------------------------------------------------------------------------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RestaurantManagement.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Kiểm tra lại tài khoản mỗi request: bị khóa/xóa, đổi vai trò hoặc đặt lại mật khẩu
        // → từ chối cookie cũ, buộc đăng nhập lại.
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                var userIdText = principal is null
                    ? null
                    : principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (principal is null || !int.TryParse(userIdText, out var userId))
                {
                    await RejectPrincipalAsync(context);
                    return;
                }

                var authenticationService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserAuthenticationService>();

                var state = await authenticationService.GetSessionStateAsync(
                    userId,
                    context.HttpContext.RequestAborted);

                var roleCode = principal.FindFirstValue(ClaimTypes.Role);
                var securityStamp = principal.FindFirstValue("auth_stamp");

                if (state is null ||
                    !state.IsActive ||
                    !string.Equals(state.RoleCode, roleCode, StringComparison.Ordinal) ||
                    !string.Equals(state.SecurityStamp.ToString(), securityStamp, StringComparison.Ordinal))
                {
                    await RejectPrincipalAsync(context);
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Chính sách dùng lại ở các phase sau (nhân viên, thực đơn, hóa đơn, báo cáo...)
    options.AddPolicy("Management", policy => policy.RequireRole("ADMIN", "OWNER", "MANAGER"));
    options.AddPolicy("Cashier", policy => policy.RequireRole("ADMIN", "OWNER", "MANAGER", "CASHIER"));
    options.AddPolicy("Kitchen", policy => policy.RequireRole("ADMIN", "OWNER", "MANAGER", "KITCHEN"));
    options.AddPolicy("PaymentManagement", policy =>
        policy.RequireRole("MANAGER", "CASHIER"));
    options.AddPolicy("OrderManagement", policy =>
        policy.RequireRole("ADMIN", "OWNER", "MANAGER", "WAITER", "CASHIER"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ---------------------------------------------------------------------------------
// Health check tạm thời cho Phase 1: xác nhận ứng dụng kết nối được SQL Server.
// Sẽ được thay bằng ASP.NET Core HealthChecks ở giai đoạn sau.
// ---------------------------------------------------------------------------------
app.MapGet("/health/db", async (RestaurantDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();

        if (!canConnect)
        {
            return Results.Json(new { status = "unhealthy", database = "RestaurantManagement" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var employees = await db.Employees.CountAsync(cancellationToken: default);

        return Results.Json(new
        {
            status = "healthy",
            database = "RestaurantManagement",
            employees
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", error = ex.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).WithName("HealthDb");

app.Run();

static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
{
    context.RejectPrincipal();
    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
