# Implementation Log — Phase 3: Authentication (2026-10-27)

> Nối tiếp `13_ImplementationLog_2026-10-27.md` (Phase 1 — khởi tạo solution + scaffold EF Core).
> Phiên này: **đăng nhập / đăng xuất / đổi mật khẩu**, cookie authentication, policy theo vai trò.
> **Phiên tiếp theo:** `15_ImplementationLog_2026-10-27_Phase4.md` (quản lý nhân viên).

## 1. Thông tin phiên

| Hạng mục | Nội dung |
|----------|----------|
| Ngày | 2026-10-27 |
| Phạm vi | Phase 3 — Authentication |
| Kết quả | Build **0 warning / 0 error**; 10/10 kịch bản kiểm thử đạt |
| Môi trường | .NET 10.0.401, EF Core 10.0.14, SQL Server 2022 (container healthy) |
| Thời lượng | ~45 phút |

## 2. Mục tiêu

| # | Yêu cầu | Kết quả |
|---|---------|---------|
| 1 | Đăng nhập bằng tài khoản trong bảng `AppUser` | ✅ |
| 2 | Đăng xuất, xóa phiên | ✅ |
| 3 | Đổi mật khẩu (xác thực mật khẩu hiện tại) | ✅ |
| 4 | Phân quyền theo vai trò (ADMIN/OWNER/MANAGER/CASHIER/WAITER/KITCHEN) | ✅ policy + UI |
| 5 | Không lưu mật khẩu dạng plaintext | ✅ PBKDF2-HMAC-SHA512 |
| 6 | Chống CSRF cho form POST | ✅ `[ValidateAntiForgeryToken]` |
| 7 | Trang yêu cầu đăng nhập phải chuyển hướng về Login | ✅ |

## 3. Kiến trúc đã triển khai

```
Web (Controller/View)
  AccountController ──► IUserAuthenticationService  (Application)
  HomeController [Authorize]                │
  CurrentUserService : ICurrentUserService  │
                                            ▼
                          UserAuthenticationService (Infrastructure)
                             ├─ RestaurantDbContext
                             └─ IPasswordHasher<AppUser>  (ASP.NET Identity)
                                            │
                                            ▼
                                   AppUser / Role / Employee
```

### Luồng đăng nhập

1. `GET /Account/Login` → form + antiforgery token.
2. `POST` → validate model → `ValidateCredentialsAsync(username, password)`.
3. Service: tìm `AppUser` chưa xóa, `IsActive = 1`, verify hash, cập nhật `LastLoginAt` (UTC).
4. Controller tạo `ClaimsIdentity` gồm: `NameIdentifier` (UserId), `Name`, `Role`, `role_name`, `employee_id`, `GivenName`.
5. `HttpContext.SignInAsync` → cookie `RestaurantManagement.Auth` (HttpOnly, SameSite=Lax).
6. Chuyển về `ReturnUrl` (nếu là URL nội bộ) hoặc `/`.

### Claims

| Claim | Nguồn | Dùng ở đâu |
|-------|-------|-----------|
| `NameIdentifier` | `AppUser.Id` | `ICurrentUserService.UserId` |
| `Name` | `AppUser.Username` | hiển thị, audit |
| `Role` | `Role.Code` | `[Authorize(Roles=...)]`, policy |
| `role_name` | `Role.Name` | hiển thị tiếng Việt |
| `employee_id` | `AppUser.EmployeeId` | gắn nghiệp vụ nhân viên |
| `GivenName` | `Employee.FullName` | lời chào |

### Policy đã đăng ký

| Policy | Vai trò được phép | Dùng từ |
|--------|-------------------|---------|
| `Management` | ADMIN, OWNER, MANAGER | Phase 4 (nhân viên, thực đơn, báo cáo) |
| `Cashier` | + CASHIER | Phase 6–7 (gọi món, thanh toán) |
| `Kitchen` | + KITCHEN | Phase 6 (màn hình bếp) |

## 4. Kết quả kiểm chứng

Chạy app tại `http://localhost:5010`, kiểm thử bằng `Invoke-WebRequest` + `WebRequestSession` (giữ cookie, gửi kèm antiforgery token).

| # | Kịch bản | Kỳ vọng | Thực tế | Kết quả |
|---|----------|---------|---------|:-------:|
| 1 | `GET /` khi chưa đăng nhập | 302 → `/Account/Login?ReturnUrl=%2F` | đúng | ✅ |
| 2 | `GET /Account/Login` | 200 + có `__RequestVerificationToken` | 200, token 155 ký tự | ✅ |
| 3 | Sai mật khẩu (`admin` / `wrong`) | 200 + thông báo lỗi chung | có "Tên đăng nhập hoặc mật khẩu không đúng..." | ✅ |
| 4 | Đúng (`admin` / `Admin@123`) | 302 → `/`, hiển thị tên + vai trò | URL `/`, có "Xin chào", badge `ADMIN` | ✅ |
| 5 | `GET /Account/ChangePassword` khi đã đăng nhập | 200 | 200 | ✅ |
| 6 | `POST /Account/Logout` | 302 → Login, phiên bị hủy | 302 → `/Account/Login` | ✅ |
| 7 | `GET /` sau khi logout | 302 → Login | 302 | ✅ |
| 8 | Tài khoản `WAITER` (tạo tạm `testauth`) | Không thấy menu quản lý/báo cáo | chỉ thấy "Gọi món", "Thanh toán"; badge "Phục vụ" | ✅ |
| 9 | Đổi mật khẩu `testauth`: `Admin@123` → `Test@12345` | Đăng nhập lại bằng mật khẩu mới OK, mật khẩu cũ bị từ chối | đúng | ✅ |
| 10 | `LastLoginAt` sau đăng nhập | Có giá trị UTC | `2026-10-27 02:29:19` (UTC) | ✅ |

**Kiểm tra CSDL sau test:** tài khoản tạm `testauth` đã xóa; còn đúng 2 tài khoản seed (`admin`, `manager`).

**Build:**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## 5. Quyết định kỹ thuật

| # | Quyết định | Lý do | Hệ quả |
|---|-----------|-------|--------|
| 1 | **Cookie Authentication + `PasswordHasher<AppUser>`** thay vì cài ASP.NET Identity đầy đủ (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`...) | CSDL đã thiết kế `AppUser` (N-1 `Role`) + seed hash tương thích Identity; cài Identity đầy đủ phải thêm 6 bảng, đổi FK (`AuditLog`, `Position.DefaultRoleId`) và sửa `docs/11` | Giữ nguyên 25 bảng; không có 2FA/lockout/external login của Identity |
| 2 | Tên interface `IUserAuthenticationService` | Trùng tên với `Microsoft.AspNetCore.Authentication.IAuthenticationService` → lỗi CS0104 | Đặt tên rõ nghĩa, tránh phải alias |
| 3 | Policy khai báo sẵn ở `Program.cs` | Dùng lại ngay từ Phase 4 | Hiện chưa có controller nào dùng policy (UI dùng `User.IsInRole`) |
| 4 | Đổi mật khẩu thành công → **đăng xuất** | Cookie cũ vẫn hợp lệ sau khi đổi mật khẩu; buộc đăng nhập lại để xác nhận mật khẩu mới | UX thêm 1 bước, an toàn hơn |
| 5 | Cookie: HttpOnly, SameSite=Lax, `SlidingExpiration`, 8 giờ; "Ghi nhớ" 7 ngày | Cân bằng an toàn và tiện dụng | Chưa bật `Cookie.SecurePolicy` vì dev chạy HTTP |
| 6 | Thông báo lỗi đăng nhập **chung** cho mọi trường hợp | Không tiết lộ tài khoản tồn tại / bị khóa | Người dùng không biết lý do cụ thể (chấp nhận được) |
| 7 | `PasswordHasher` được đăng ký DI (`IPasswordHasher<AppUser>`) | Tách khỏi service, dễ thay thế/test | Có thể mock khi viết unit test |

### Nếu bắt buộc dùng ASP.NET Identity đầy đủ

1. Thêm 6 bảng `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserTokens` (script mới trong `database/`).
2. `ApplicationUser : IdentityUser<int>` + `ApplicationRole : IdentityRole<int>`, map `AppUser`/`Role` cũ hoặc migrate dữ liệu.
3. Đổi FK `AuditLog.ChangedByUserId` và `Position.DefaultRoleId` sang bảng mới.
4. Cập nhật `docs/11` (số bảng 25 → 31) và `database/README.md`.

## 6. Lỗi gặp phải & cách xử lý

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|-----|-------------|-----------|
| 1 | `CS0104: IAuthenticationService is ambiguous` | Trùng tên với interface của ASP.NET Core | Đổi thành `IUserAuthenticationService` |
| 2 | `MSB3021/MSB3027: file is locked by RestaurantManagement.Web` | App đang chạy giữ DLL | Dừng process cổng 5010 rồi build lại |
| 3 | Kiểm thử báo sai "không thấy menu quản lý" | Razor HTML-encode ký tự tiếng Việt trong nội dung động (`T&#xEA;n...`) → so khớp chuỗi thất bại | `[System.Net.WebUtility]::HtmlDecode($content)` trước khi so khớp |
| 4 | `INSERT AppUser` lỗi `QUOTED_IDENTIFIER` | `sqlcmd` mặc định OFF, bảng có filtered index | Thêm cờ `-I` khi chạy `sqlcmd` |
| 5 | Test tạo tài khoản tạm có thể sót dữ liệu | Cần tài khoản vai trò WAITER để test phân quyền | Xóa cứng sau test + kiểm tra lại danh sách tài khoản |
| 6 | Đăng xuất bằng `GET` không được | Logout là POST + antiforgery (đúng chuẩn) | Gọi `POST /Account/Logout` kèm token (đã ghi trong mục 4) |

## 7. Tệp đã tạo / cập nhật

| Tệp | Trạng thái | Ghi chú |
|-----|-----------|---------|
| `src/RestaurantManagement.Application/Abstractions/IUserAuthenticationService.cs` | Mới | Hợp đồng xác thực |
| `src/RestaurantManagement.Application/Models/AuthenticatedUser.cs` | Mới | DTO kết quả xác thực |
| `src/RestaurantManagement.Infrastructure/Authentication/UserAuthenticationService.cs` | Mới | Verify hash, cập nhật `LastLoginAt`, đổi mật khẩu |
| `src/RestaurantManagement.Infrastructure/DependencyInjection.cs` | Cập nhật | Đăng ký `IPasswordHasher<AppUser>`, `IUserAuthenticationService` |
| `src/RestaurantManagement.Web/Controllers/AccountController.cs` | Mới | Login / Logout / ChangePassword / AccessDenied |
| `src/RestaurantManagement.Web/Services/CurrentUserService.cs` | Mới | Đọc claim |
| `src/RestaurantManagement.Web/Models/LoginViewModel.cs` | Mới | Validate form đăng nhập |
| `src/RestaurantManagement.Web/Models/ChangePasswordViewModel.cs` | Mới | Validate đổi mật khẩu |
| `src/RestaurantManagement.Web/Views/Account/Login.cshtml` | Mới | Form đăng nhập + tài khoản mẫu |
| `src/RestaurantManagement.Web/Views/Account/ChangePassword.cshtml` | Mới | Form đổi mật khẩu |
| `src/RestaurantManagement.Web/Views/Account/AccessDenied.cshtml` | Mới | Trang 403 |
| `src/RestaurantManagement.Web/Views/Shared/_Layout.cshtml` | Cập nhật | Menu theo trạng thái đăng nhập/role, nút đăng xuất |
| `src/RestaurantManagement.Web/Views/Home/Index.cshtml` | Cập nhật | Trang chủ theo vai trò |
| `src/RestaurantManagement.Web/Controllers/HomeController.cs` | Cập nhật | `[Authorize]`, `[AllowAnonymous]` cho Error |
| `src/RestaurantManagement.Web/Program.cs` | Cập nhật | Cookie auth, policy, `UseAuthentication()` |
| `src/RestaurantManagement.Infrastructure/*.csproj` | Cập nhật | Thêm `Microsoft.Extensions.Identity.Core` 10.0.14 |
| `docs/03_UserRoles.md` | Cập nhật | 6 vai trò + ma trận quyền (khớp CSDL) |
| `docs/07_TechnologyStack.md` | Cập nhật | Ghi chú cơ chế xác thực |
| `docs/09_DevelopmentRoadmap.md` | Cập nhật | Phase 3 ✅ |
| `docs/12`, `docs/13` | Cập nhật | Liên kết nhật ký |
| `src/README.md` | Cập nhật | Hướng dẫn đăng nhập |

## 8. Cách chạy & kiểm tra nhanh

```bash
cd docker && docker compose up -d
cd ../src && dotnet run --project RestaurantManagement.Web
```

Mở `http://localhost:5010` → tự chuyển về trang đăng nhập.

| Tài khoản | Mật khẩu | Vai trò |
|-----------|----------|---------|
| `admin` | `Admin@123` | ADMIN |
| `manager` | `Admin@123` | MANAGER |

Kiểm tra DB:

```bash
docker exec restaurant-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '<MSSQL_SA_PASSWORD>' -C -d RestaurantManagement -I \
  -Q "SELECT Username, IsActive, LastLoginAt FROM AppUser"
```

## 9. Việc còn lại

| # | Việc | Ưu tiên |
|---|------|:-------:|
| 1 | Khóa tài khoản tạm sau N lần sai (lockout) | Trung bình |
| 2 | Ghi `AuditLog` cho đăng nhập / đổi mật khẩu | Trung bình |
| 3 | Trang quản trị tài khoản (tạo user, reset mật khẩu, khóa) | Cao (Phase 4) |
| 4 | Bật `Cookie.SecurePolicy = Always` khi chạy HTTPS/production | Cao |
| 5 | Unit test cho `UserAuthenticationService` (đúng/sai/khóa/đổi mật khẩu) | Trung bình |
| 6 | Quyết định với giảng viên: cookie auth (hiện tại) hay Identity đầy đủ | Cao |
| 7 | Áp policy `Management`/`Cashier`/`Kitchen` cho controller thật | Phase 4–7 |
| 8 | Quên mật khẩu (gửi email) — ngoài phạm vi đồ án, có thể bỏ | Thấp |

## 10. Ghi chú cập nhật tài liệu

1. Thêm vai trò → sửa `docs/03`, `database/02_seed.sql`, policy trong `Program.cs`.
2. Đổi cơ chế xác thực → cập nhật `docs/07`, mục 5 file này và `docs/11` nếu schema đổi.
3. Mọi kịch bản kiểm thử mới ghi vào mục 4 (kỳ vọng vs thực tế).
4. Không đưa mật khẩu thật vào tài liệu/triển khai; mật khẩu seed chỉ dùng cho môi trường dev.
