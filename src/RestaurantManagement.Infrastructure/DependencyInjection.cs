using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Accounts;
using RestaurantManagement.Infrastructure.Catalog;
using RestaurantManagement.Infrastructure.Timekeeping;
using RestaurantManagement.Infrastructure.Authentication;
using RestaurantManagement.Infrastructure.Employees;
using RestaurantManagement.Infrastructure.Persistence;
using RestaurantManagement.Infrastructure.Schedules;
using RestaurantManagement.Infrastructure.Shifts;
using RestaurantManagement.Infrastructure.Tables;
using RestaurantManagement.Infrastructure.Orders;
using RestaurantManagement.Infrastructure.Payments;
using RestaurantManagement.Infrastructure.Reporting;
using RestaurantManagement.Infrastructure.Auditing;

namespace RestaurantManagement.Infrastructure;

/// <summary>
/// Điểm đăng ký duy nhất cho tầng Infrastructure (DbContext, repository, service triển khai).
/// Web chỉ cần gọi <c>builder.Services.AddInfrastructure(builder.Configuration)</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Chuỗi kết nối 'DefaultConnection' chưa được cấu hình trong appsettings.json.");
        }

        services.AddDbContext<RestaurantDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(RestaurantDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // ASP.NET Identity password hashing (PBKDF2-HMAC-SHA512), dùng chung với dữ liệu seed trong database/02_seed.sql
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

        // Tầng nghiệp vụ xác thực
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();

        // Quản lý tài khoản (chỉ ADMIN)
        services.AddScoped<IAccountService, AccountService>();

        // Nghiệp vụ nhân sự
        services.AddScoped<IEmployeeService, EmployeeService>();

        // Ca làm, xếp ca, chấm công
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAttendanceService, AttendanceService>();

        // Thực đơn
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IMenuService, MenuService>();

        // Quản lý khu vực và bàn ăn
        services.AddScoped<ITableManagementService, TableManagementService>();

        // Gọi món và xử lý hàng đợi bếp
        services.AddScoped<IOrderService, OrderService>();

        // Thanh toán và đóng hóa đơn
        services.AddScoped<IPaymentService, PaymentService>();

        // Báo cáo doanh thu, khách, món bán và công/lương
        services.AddScoped<IReportingService, ReportingService>();

        // Nhật ký thay đổi dữ liệu và lịch sử chuyển/gộp bàn
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
