# Implementation Log — Phase 4c: Quản lý tài khoản (2026-10-27)

> Nối tiếp `16_ImplementationLog_2026-10-27_Phase4b.md` (ca làm & chấm công).
> Phiên này: **cấp tài khoản cho nhân viên, đổi vai trò, đặt lại mật khẩu, khóa/mở**,
> kèm kiểm tra lại phiên đăng nhập mỗi request (khóa/đổi quyền có hiệu lực ngay).

## 1. Thông tin phiên

| Hạng mục | Nội dung |
|----------|----------|
| Ngày | 2026-10-27 |
| Phạm vi | Phase 4c — Account Management (`AppUser` / `Role`) |
| Kết quả | Build **0 warning / 0 error**; 36/36 bước kiểm thử đạt |
| Môi trường | .NET 10, EF Core 10, SQL Server 2022 (container healthy) |
| Thời lượng | ~45 phút |

## 2. Mục tiêu & kết quả

| # | Yêu cầu | Kết quả |
|---|---------|---------|
| 1 | Danh sách tài khoản + tìm kiếm + lọc vai trò/trạng thái | ✅ |
| 2 | Tạo tài khoản cho nhân viên đang làm việc | ✅ |
| 3 | Mỗi nhân viên tối đa 1 tài khoản | ✅ (service + `UX_AppUser_EmployeeId`) |
| 4 | Đổi vai trò | ✅ |
| 5 | Đặt lại mật khẩu | ✅ |
| 6 | Khóa / mở khóa tài khoản | ✅ |
| 7 | Khóa/đổi quyền có hiệu lực với phiên đang đăng nhập | ✅ (cookie validation + security stamp) |
| 8 | Chỉ ADMIN quản lý tài khoản (docs/03) | ✅ |
| 9 | Bảo vệ tài khoản hệ thống: không tự khóa, không khóa ADMIN cuối | ✅ |
| 10 | Liên kết tạo tài khoản từ chi tiết nhân viên | ✅ |

## 3. Kiến trúc

```
AccountsController   [Authorize(Roles="ADMIN")]  → /Accounts, Create, Edit, ResetPassword, ToggleActive
   │  AccountIndexViewModel / AccountCreateViewModel / AccountEditViewModel / AccountResetPasswordViewModel
   ▼
IAccountService (Application)
   ├─ SearchAsync / GetDetailsAsync
   ├─ GetEmployeeOptionsAsync  (đang làm việc + chưa từng có tài khoản)
   ├─ GetRoleOptionsAsync
   ├─ CreateAsync
   ├─ UpdateRoleAsync
   ├─ ResetPasswordAsync
   └─ SetActiveAsync
   ▼
AccountService (Infrastructure, EF Core + PasswordHasher<AppUser>)
   └─ ICurrentUserService → CreatedBy/UpdatedBy; chặn tự thao tác trên chính mình

Cookie Authentication
   └─ OnValidatePrincipal (Program.cs)
        ├─ IUserAuthenticationService.GetSessionStateAsync(userId)
        ├─ tài khoản bị khóa/xóa, nhân viên nghỉ → từ chối phiên
        ├─ vai trò trong DB ≠ claim → từ chối phiên
        └─ security stamp (AppUser.UpdatedAt) ≠ claim "auth_stamp" → từ chối phiên
```

### Quy tắc nghiệp vụ đã cài

| # | Quy tắc | Nơi kiểm tra |
|---|---------|--------------|
| 1 | Chỉ ADMIN vào `/Accounts` | `[Authorize(Roles="ADMIN")]` |
| 2 | Chỉ tạo tài khoản cho nhân viên đang làm việc, chưa xóa | Service |
| 3 | Mỗi nhân viên tối đa 1 tài khoản (kể cả tài khoản đã xóa mềm) | Service + `UX_AppUser_EmployeeId` |
| 4 | Tên đăng nhập 3–50 ký tự, chỉ `a-z A-Z 0-9 . _ -` | ViewModel + Service |
| 5 | Tên đăng nhập duy nhất (kể cả tài khoản đã xóa mềm) | Service + `UQ_AppUser_Username` |
| 6 | Mật khẩu 6–100 ký tự, băm PBKDF2-HMAC-SHA512 | ViewModel + Service + `PasswordHasher<AppUser>` |
| 7 | Không tạo/không gán vai trò `ADMIN` qua UI | Service (ADMIN là tài khoản hệ thống) |
| 8 | Không tự khóa tài khoản đang đăng nhập | Service |
| 9 | Không khóa ADMIN đang hoạt động cuối cùng | Service (đếm ADMIN còn lại) |
| 10 | Không tự đặt lại mật khẩu / tự đổi vai trò của mình | Service + Controller |
| 11 | Không sửa/đặt lại mật khẩu tài khoản `ADMIN` | Controller + Service |
| 12 | Khóa, đổi vai trò, đặt lại mật khẩu → phiên cũ mất hiệu lực | Cookie `OnValidatePrincipal` + `auth_stamp` |
| 13 | Nhân viên nghỉ việc/bị xóa → phiên đăng nhập bị từ chối | `GetSessionStateAsync` kiểm tra `Employee.Status`/`IsDeleted` |
| 14 | Tài khoản không xóa cứng từ UI, chỉ khóa | Giữ lịch sử `CreatedBy`/`UpdatedBy` |

## 4. Kết quả kiểm chứng

Chạy app tại `http://localhost:5010`.

| # | Kịch bản | Kỳ vọng | Thực tế | Kết quả |
|---|----------|---------|---------|:-------:|
| 1 | ADMIN `GET /Accounts` | 200, 2 tài khoản seed | 200, `admin`, `manager` | ✅ |
| 2 | Dòng `admin` | Không có thao tác, ghi "Tài khoản hệ thống" | đúng | ✅ |
| 3 | Dòng `manager` | Có sửa vai trò / đặt lại mật khẩu / khóa | đúng | ✅ |
| 4 | Trang chủ ADMIN | Có card "Tài khoản" + menu | đúng | ✅ |
| 5 | MANAGER `GET /Accounts` | Bị chặn | 302 → AccessDenied | ✅ |
| 6 | `GET /Accounts/Create` | 3 nhân viên đủ điều kiện (NV002–NV004) | 3 option | ✅ |
| 7 | Dropdown vai trò | Không có ADMIN | OWNER, MANAGER, CASHIER, WAITER, KITCHEN | ✅ |
| 8 | Tạo `nv003test` cho NV003, vai trò WAITER | Thành công | "Đã tạo tài khoản 'nv003test'." | ✅ |
| 9 | Tạo trùng tên đăng nhập | Bị chặn | "Tên đăng nhập 'nv003test' đã tồn tại." | ✅ |
| 10 | Tạo cho NV001 (đã có tài khoản) | Bị chặn | "Nhân viên này đã có tài khoản đăng nhập." | ✅ |
| 11 | POST trực tiếp `RoleId=1` (ADMIN) | Bị chặn | "Không tạo tài khoản ADMIN qua màn hình này…" | ✅ |
| 12 | Mật khẩu `123` | Bị chặn | "Mật khẩu phải từ 6 đến 100 ký tự." | ✅ |
| 13 | Sau các lần lỗi, NV004 vẫn chưa có tài khoản | Không tạo rác | NV004 vẫn trong dropdown | ✅ |
| 14 | NV003 đăng nhập `nv003test` / `Test@1234` | Thành công | trang chủ "Xin chào" | ✅ |
| 15 | Vai trò hiển thị | WAITER | "Phục vụ" | ✅ |
| 16 | Menu nhân viên | Không có "Nhân viên", "Tài khoản" | đúng | ✅ |
| 17 | Nhân viên `GET /Accounts` | Bị chặn | 302 → AccessDenied | ✅ |
| 18 | Nhân viên `GET /Attendance` | 200, ca của mình | 200 | ✅ |
| 19 | ADMIN đặt lại mật khẩu `nv003test` | Thành công | "Đã đặt lại mật khẩu…" | ✅ |
| 20 | Phiên cũ của nhân viên | Bị từ chối | 302 → `/Account/Login` | ✅ |
| 21 | Đăng nhập bằng mật khẩu cũ | Thất bại | "Tên đăng nhập hoặc mật khẩu không đúng…" | ✅ |
| 22 | Đăng nhập bằng `NewPass@123` | Thành công | đúng | ✅ |
| 23 | ADMIN khóa `nv003test` | `IsActive=0` | "Đã khóa tài khoản." | ✅ |
| 24 | Phiên đang mở của nhân viên | Bị từ chối | 302 → Login | ✅ |
| 25 | Đăng nhập khi bị khóa | Thất bại | thông báo chung | ✅ |
| 26 | ADMIN mở khóa | `IsActive=1` | "Đã mở khóa tài khoản." | ✅ |
| 27 | Đăng nhập lại sau mở khóa | Thành công | đúng | ✅ |
| 28 | Đổi vai trò WAITER → CASHIER | Thành công | "Đã đổi vai trò…" | ✅ |
| 29 | Phiên cũ sau đổi vai trò | Bị từ chối | 302 → Login | ✅ |
| 30 | Đăng nhập lại | Vai trò CASHIER | badge "Thu ngân" | ✅ |
| 31 | Menu sau đổi vai trò | Không có "Tài khoản" | đúng | ✅ |
| 32 | ADMIN tự khóa mình (`/ToggleActive/1`) | Bị chặn | "Không thể khóa tài khoản đang đăng nhập." | ✅ |
| 33 | ADMIN tự đặt lại mật khẩu (`/ResetPassword/1`) | Bị chặn | "Hãy dùng chức năng Đổi mật khẩu…" | ✅ |
| 34 | ADMIN sửa vai trò chính mình (`/Edit/1`) | Bị chặn | "Tài khoản ADMIN là tài khoản hệ thống…" | ✅ |
| 35 | Chi tiết NV004 (ADMIN) | Có link "Tạo tài khoản" | đúng | ✅ |
| 36 | Chi tiết NV004 (MANAGER) | Không có link tạo tài khoản | đúng | ✅ |
| 37 | Build toàn solution | 0 warning / 0 error | đúng | ✅ |

**Dọn dữ liệu test:** đã xóa vật lý tài khoản `nv003test` (id 1005). Baseline trở lại:
**2 tài khoản (`admin`, `manager`), 4 nhân viên, 6 lịch ca, 0 chấm công, 0 audit test**.

> Kiểm thử dùng HTTP thật với cookie session + antiforgery token; mỗi lần đổi mật khẩu/khóa/đổi vai trò đều kiểm tra lại phiên cũ.

## 5. Quyết định kỹ thuật

| # | Quyết định | Lý do | Hệ quả |
|---|-----------|-------|--------|
| 1 | Chỉ ADMIN quản lý tài khoản | Đúng ma trận quyền `docs/03` | OWNER/MANAGER không cấp được tài khoản |
| 2 | Không cho tạo/gán `ADMIN` qua UI | ADMIN là tài khoản hệ thống, không gắn hồ sơ nhân viên | Muốn thêm ADMIN phải thao tác SQL có kiểm soát |
| 3 | Không xóa tài khoản, chỉ khóa | Giữ lịch sử người tạo/cập nhật, khóa có hiệu lực ngay | Tên đăng nhập/nhân viên không tái sử dụng được (unique index) |
| 4 | `EmployeeId` unique có điều kiện (đã có trong DB) | 1 nhân viên = 1 tài khoản | Tài khoản xóa mềm vẫn chiếm chỗ (đúng nghiệp vụ lưu trữ) |
| 5 | **Security stamp = `AppUser.UpdatedAt`** thay vì thêm cột `SecurityStamp` | Không đổi schema database-first; `LastLoginAt` cập nhật riêng nên không làm rớt phiên | Đổi mật khẩu/khóa/đổi vai trò đều đăng xuất phiên cũ; độ chính xác 1 giây |
| 6 | Kiểm tra phiên mỗi request (`OnValidatePrincipal`) | Khóa tài khoản phải có hiệu lực ngay, không chờ cookie hết hạn | Thêm 1 truy vấn nhẹ/request; có thể cache sau này |
| 7 | Nhân viên nghỉ việc/bị xóa cũng làm mất hiệu lực phiên | `EmployeeService` đã khóa tài khoản, đây là lớp bảo vệ thứ 2 | Tránh trường hợp sửa DB trực tiếp |
| 8 | Mật khẩu tối thiểu 6 ký tự (đồng bộ Đổi mật khẩu) | Nhất quán toàn hệ thống | Có thể nâng lên 8 + độ phức tạp ở phase bảo mật |
| 9 | Đổi vai trò bắt đăng nhập lại | Claim vai trò nằm trong cookie | Tránh quyền cũ tồn tại đến 8 giờ |
| 10 | ADMIN tự thao tác trên mình bị chặn | Tránh tự khóa/tự hạ quyền ngoài ý muốn | Dùng "Đổi mật khẩu" cho mật khẩu của mình |

## 6. Lỗi gặp phải & cách xử lý

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|-----|-------------|-----------|
| 1 | `CS8604` tại `OnValidatePrincipal` | `ClaimsPrincipal` có thể null | Kiểm tra `principal is null` trước khi đọc claim |
| 2 | Trang chủ tạm mất card "Gọi món" khi chèn card tài khoản | Sửa block HTML thiếu phần đóng | Khôi phục card + thêm card mới, build lại |
| 3 | `$home` báo "read-only or constant" (lặp lại từ Phase 4) | `$HOME` là biến dành riêng của PowerShell | Đổi tên biến test thành `$homePage` |
| 4 | Test kiểm tra vai trò bằng `WAITER` báo sai | Trang chủ hiển thị tên vai trò tiếng Việt ("Phục vụ") | Kiểm tra đúng nhãn hiển thị |
| 5 | Test "phiên cũ bị từ chối" báo lỗi 302 | `Invoke-WebRequest` không theo redirect khi cookie bị từ chối | Xác nhận bằng mã 302 + `Location: /Account/Login` (đúng kỳ vọng) |

## 7. Tệp đã tạo / cập nhật

| Tệp | Trạng thái | Ghi chú |
|-----|-----------|---------|
| `Application/Models/AuthenticatedUser.cs` | Cập nhật | Thêm `SecurityStamp` |
| `Application/Models/UserSessionState.cs` | Mới | Trạng thái phiên để kiểm tra cookie |
| `Application/Abstractions/IUserAuthenticationService.cs` | Cập nhật | Thêm `GetSessionStateAsync` |
| `Application/Models/Accounts/AccountDtos.cs` | Mới | 5 DTO |
| `Application/Abstractions/IAccountService.cs` | Mới | 8 phương thức |
| `Infrastructure/Accounts/AccountService.cs` | Mới | Toàn bộ quy tắc tài khoản |
| `Infrastructure/Authentication/UserAuthenticationService.cs` | Cập nhật | Security stamp + trạng thái phiên |
| `Infrastructure/DependencyInjection.cs` | Cập nhật | Đăng ký `IAccountService` |
| `Web/Program.cs` | Cập nhật | `OnValidatePrincipal` + từ chối phiên |
| `Web/Controllers/AccountController.cs` | Cập nhật | Claim `auth_stamp` |
| `Web/Controllers/AccountsController.cs` | Mới | Index/Create/Edit/ResetPassword/ToggleActive |
| `Web/Models/AccountViewModels.cs` | Mới | 4 view model + DataAnnotations tiếng Việt |
| `Web/Views/Accounts/Index.cshtml` | Mới | Bảng + lọc + thao tác |
| `Web/Views/Accounts/Create.cshtml` | Mới | Cấp tài khoản |
| `Web/Views/Accounts/Edit.cshtml` | Mới | Đổi vai trò |
| `Web/Views/Accounts/ResetPassword.cshtml` | Mới | Đặt lại mật khẩu |
| `Web/Views/Employees/Details.cshtml` | Cập nhật | Link tạo/quản lý tài khoản (ADMIN) |
| `Web/Views/Shared/_Layout.cshtml` | Cập nhật | Menu "Tài khoản" (ADMIN) |
| `Web/Views/Home/Index.cshtml` | Cập nhật | Card "Tài khoản" (ADMIN) |
| `Web/Views/_ViewImports.cshtml` | Cập nhật | Namespace Accounts |
| `docs/09_DevelopmentRoadmap.md` | Cập nhật | Ghi nhận Phase 4c |
| `docs/15_..._Phase4.md` | Cập nhật | Đánh dấu việc quản lý tài khoản hoàn thành |
| `docs/03_UserRoles.md` | Cập nhật | Ghi chú màn hình `/Accounts` |
| `src/README.md` | Cập nhật | Chức năng + cơ chế kiểm tra phiên |

## 8. Hạn chế & việc còn lại

| # | Việc | Ưu tiên |
|---|------|:-------:|
| 1 | Giới hạn số lần đăng nhập sai / tạm khóa chống brute-force | Cao |
| 2 | Quên mật khẩu (email/OTP) — hiện chỉ ADMIN đặt lại | Trung bình |
| 3 | Audit trigger cho `AppUser` (không ghi PasswordHash hoặc lần đăng nhập) | Hoàn tất trong `docs/22_ImplementationLog_2026-10-28_Audit.md` |
| 4 | Gắn `ChangedByUserId` cho `AuditLog` (SESSION_CONTEXT) | Hoàn tất trong `docs/22_ImplementationLog_2026-10-28_Audit.md` |
| 5 | Trang "Tài khoản của tôi" (xem vai trò, lần đăng nhập cuối) | Thấp |
| 6 | Cho phép đổi tên đăng nhập (hiện cố định sau khi tạo) | Thấp |
| 7 | Cache kết quả kiểm tra phiên 30–60 giây để giảm tải DB | Thấp |
| 8 | Unit test `AccountService` (unique, tự khóa, ADMIN cuối) | Trung bình |
| 9 | Nâng chính sách mật khẩu (≥ 8 ký tự, chữ + số + ký tự đặc biệt) | Trung bình |
| 10 | Nâng cấp lên ASP.NET Identity đầy đủ nếu đồ án yêu cầu | Thấp (đã có lộ trình ở docs/14) |

## 9. Cách chạy

```bash
cd docker && docker compose up -d
cd ../src && dotnet run --project RestaurantManagement.Web
# http://localhost:5010 → admin / Admin@123 → menu "Tài khoản"
```

## 10. Ghi chú cập nhật tài liệu

1. Thêm trường tài khoản → cập nhật `docs/11` (bảng `AppUser`), entity, DTO, view model, service.
2. Đổi quy tắc tài khoản → sửa mục 3 file này + `docs/03` (ma trận quyền).
3. Kịch bản kiểm thử mới → bổ sung mục 4.
4. Mọi thay đổi vai trò/khóa tài khoản phải kiểm tra lại luồng cookie (mục 3 quy tắc 12–13).
5. Dữ liệu test phải dọn sạch trước khi kết thúc phiên (như `nv003test` ở phiên này).
