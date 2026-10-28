# Source — Restaurant Management System

Solution ASP.NET Core MVC theo **Clean Architecture**, ánh xạ trực tiếp tới CSDL trong `../database`.

## Cấu trúc

```
src/
├── RestaurantManagement.slnx
├── RestaurantManagement.Domain/              # Entity (không phụ thuộc project nào)
│   └── Entities/                             # 35 lớp: 25 bảng + 10 view (scaffold từ DB)
├── RestaurantManagement.Application/         # DTO, interface service (không phụ thuộc EF)
│   ├── Abstractions/                         # Service contracts, gồm IOrderService và IPaymentService
│   ├── Common/                               # PagedResult<T>, OperationResult<T>
│   └── Models/                               # AuthenticatedUser, UserSessionState, Employees/*.cs, Shifts/*.cs, Schedules/*.cs, Attendance/*.cs, Accounts/*.cs, Orders/*.cs, Payments/*.cs
├── RestaurantManagement.Infrastructure/      # EF Core DbContext + service triển khai
│   ├── Authentication/UserAuthenticationService.cs
│   ├── Accounts/AccountService.cs            # Tài khoản: tạo, đổi vai trò, reset mật khẩu, khóa/mở
│   ├── Employees/EmployeeService.cs
│   ├── Shifts/ShiftService.cs                # Ca làm + phân ca nhân viên
│   ├── Schedules/ScheduleService.cs          # Xếp ca theo ngày
│   ├── Timekeeping/AttendanceService.cs      # Check-in/out + duyệt công
│   ├── Catalog/                               # Nhóm món + thực đơn
│   ├── Tables/TableManagementService.cs       # Khu vực + bàn ăn
│   ├── Orders/OrderService.cs                 # Mở bàn, hóa đơn, gọi món, chuyển/gộp bàn
│   ├── Payments/PaymentService.cs             # Tính tiền, thanh toán, đóng hóa đơn và biên nhận
│   ├── Reporting/ReportingService.cs           # Báo cáo doanh thu, khách, món bán và lương
│   ├── Persistence/RestaurantDbContext.cs
│   └── DependencyInjection.cs
└── RestaurantManagement.Web/                 # MVC: Controller, View, Program.cs (composition root)
    ├── Controllers/AccountController.cs      # Login / Logout / ChangePassword
    ├── Controllers/EmployeesController.cs    # CRUD nhân viên (policy Management)
    ├── Controllers/ShiftsController.cs       # Ca làm + phân ca (policy Management)
    ├── Controllers/SchedulesController.cs    # Lịch ca (policy Management)
    ├── Controllers/AttendanceController.cs   # Chấm công (mọi vai trò)
    ├── Controllers/AccountsController.cs     # Tài khoản (chỉ ADMIN)
    ├── Controllers/MenuController.cs         # Xem thực đơn; quản lý món theo vai trò
    ├── Controllers/CategoriesController.cs   # Nhóm món (policy Management)
    ├── Controllers/TablesController.cs       # Sơ đồ bàn + khu vực (quản lý theo policy)
    ├── Controllers/OrdersController.cs      # Mở hóa đơn, thêm/đổi/hủy món, chuyển/gộp
    ├── Controllers/PaymentsController.cs    # Danh sách thanh toán, checkout và biên nhận
    ├── Controllers/ReportsController.cs    # Dashboard báo cáo, bộ lọc và xuất CSV
    ├── Controllers/KitchenController.cs     # Hàng đợi bếp FIFO + trạng thái món
    ├── Helpers/EmployeeDisplay.cs
    ├── Services/CurrentUserService.cs
    └── Views/Account/, Views/Accounts/, Views/Employees/, Views/Shifts/, Views/Schedules/, Views/Attendance/, Views/Payments/
```

**Chiều phụ thuộc:** `Web → Infrastructure → Application → Domain`.

> `ICurrentUserService` được khai báo ở Application nhưng **triển khai ở Web** (`CurrentUserService` dùng `IHttpContextAccessor`).
> Vì vậy Infrastructure phụ thuộc interface này và chỉ hoạt động khi chạy qua Web (DI đăng ký đủ). Khi viết unit test cần mock `ICurrentUserService`.

## Chạy

```bash
cd src
dotnet build
dotnet run --project RestaurantManagement.Web
```

- Yêu cầu SQL Server đang chạy: `cd ../docker && docker compose up -d`
- Cấu hình chuỗi kết nối bằng User Secrets (không lưu mật khẩu trong `appsettings.json`):

```powershell
cd src\RestaurantManagement.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=RestaurantManagement;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Thay `<MSSQL_SA_PASSWORD>` bằng mật khẩu đã đặt trong `docker/.env`.
- Ứng dụng chạy tại `http://localhost:5010` (theo `launchSettings.json`)

## Chức năng hiện có

| Nhóm | Chức năng | Vai trò |
|------|-----------|---------|
| Xác thực | Đăng nhập, đăng xuất, đổi mật khẩu | Tất cả |
| Thực đơn | Xem món theo nhóm, tìm kiếm, giá bán, trạng thái còn/hết | Tất cả |
| Thực đơn | Thêm/sửa/xóa mềm món, ảnh, đơn vị và mô tả | ADMIN, OWNER, MANAGER |
| Nhóm món | Thêm/sửa/ngừng/kích hoạt/xóa mềm, sắp xếp thứ tự | ADMIN, OWNER, MANAGER |
| Bàn ăn | Xem sơ đồ bàn và trạng thái (trống/có khách/đợi món/món xong) | Tất cả |
| Bàn ăn | Quản lý khu vực, bàn, sức chứa; chặn xóa/ngừng bàn đang có khách | ADMIN, OWNER, MANAGER |
| Gọi món | Mở bàn/hóa đơn, nhập khách, gọi thêm món, lưu giá tại thời điểm gọi | ADMIN, OWNER, MANAGER, WAITER, CASHIER |
| Gọi món | Hủy món chờ hoặc đổi món nguyên tử (lưu lý do); không hủy món bếp đã nhận | ADMIN, OWNER, MANAGER, WAITER, CASHIER |
| Gọi món | Chuyển bàn trống; gộp hóa đơn và giữ nhật ký chuyển/gộp bàn | ADMIN, OWNER, MANAGER, WAITER, CASHIER |
| Bếp | Hàng đợi FIFO, nhận chế biến và đánh dấu hoàn thành | ADMIN, OWNER, MANAGER, KITCHEN |
| Thanh toán | Tính tiền, tiền mặt/chuyển khoản, đóng hóa đơn, giải phóng bàn, in biên nhận | MANAGER, CASHIER |
| Thanh toán | Nhập giảm giá và xác nhận thanh toán khi còn món chưa hoàn thành | Giảm giá: MANAGER; xác nhận: MANAGER, CASHIER |
| Báo cáo | Doanh thu, khách theo ca/nhân viên/bàn, món bán, công đã duyệt và lương | ADMIN, OWNER, MANAGER |
| Nhân viên | Danh sách, tìm kiếm/lọc/phân trang, thêm, sửa, chi tiết, xóa mềm | ADMIN, OWNER, MANAGER |
| Ca làm | Danh sách/thêm/sửa ca, ngừng–kích hoạt, phân ca cho nhân viên | ADMIN, OWNER, MANAGER |
| Lịch ca | Xếp ca theo ngày, xem theo khoảng ngày, lọc nhân viên, xóa lịch ca | ADMIN, OWNER, MANAGER |
| Chấm công | Xem ca trong ngày, check-in/check-out ca của mình | Tất cả (có hồ sơ nhân viên) |
| Chấm công | Xem toàn bộ nhân viên, lọc, duyệt công | ADMIN, OWNER, MANAGER |
| Tài khoản | Danh sách/lọc, cấp tài khoản, đổi vai trò, đặt lại mật khẩu, khóa/mở | ADMIN |

### Tài khoản mẫu

| Tài khoản | Mật khẩu | Vai trò |
|-----------|----------|---------|
| `admin` | `Admin@123` | ADMIN |
| `manager` | `Admin@123` | MANAGER |

- Chưa đăng nhập: mọi trang (trừ Login/Health) chuyển hướng về `/Account/Login`.
- Đổi mật khẩu: menu **Đổi mật khẩu** → thành công sẽ đăng xuất và yêu cầu đăng nhập lại.
- Kiểm tra kết nối DB: `GET http://localhost:5010/health/db` (ẩn danh).

### Quy tắc nghiệp vụ nhân viên

- Mã nhân viên và CCCD/CMND duy nhất (kể cả nhân viên đã xóa mềm).
- Trạng thái **Đã nghỉ** bắt buộc có ngày nghỉ; ngày nghỉ ≥ ngày vào làm.
- Xóa = **xóa mềm**; đồng thời khóa tài khoản đăng nhập (nếu có).
- Không thể xóa nhân viên đang đăng nhập.

### Quy tắc ca làm & chấm công

- Nhân viên phải được **phân ca** (`EmployeeShift`) trước khi xếp lịch.
- Không xếp ca cho ngày đã qua, không trùng ca, không trùng giờ với ca khác trong cùng ngày.
- Xếp lại ca đã xóa mềm sẽ **khôi phục** bản ghi cũ (giữ `Id`).
- Chỉ check-in ca của mình, đúng ngày hôm nay, mỗi ca một lần; check-out phải sau check-in.
- Quản lý duyệt công (`Attendance.Status: 0 → 1`); báo cáo/lương sau này chỉ tính công **đã duyệt**.
- Không xóa lịch ca khi đã phát sinh chấm công.

### Quy tắc tài khoản

- Chỉ **ADMIN** được cấp/quản lý tài khoản; mỗi nhân viên tối đa 1 tài khoản.
- Tên đăng nhập duy nhất (kể cả tài khoản đã xóa mềm), 3–50 ký tự, chỉ `a-z A-Z 0-9 . _ -`.
- Không tạo/gán vai trò `ADMIN` qua UI (ADMIN là tài khoản hệ thống).
- Không tự khóa/reset mật khẩu/đổi vai trò của chính mình; không khóa ADMIN hoạt động cuối cùng.
- Khóa, đổi vai trò hoặc đặt lại mật khẩu → **phiên đăng nhập cũ mất hiệu lực ngay** (kiểm tra mỗi request).
- Tài khoản không xóa cứng từ UI, chỉ khóa.

### Quy tắc thực đơn và bàn

- Mã nhóm/mã món duy nhất, kể cả bản ghi đã xóa mềm; thao tác xóa trên giao diện là xóa mềm.
- Quản lý có thể khôi phục món đã xóa mềm vào một nhóm đang hoạt động từ chế độ **Món đã xóa** trong trang thực đơn.
- Món thuộc nhóm đang sử dụng; đổi giá chỉ ảnh hưởng hóa đơn tạo sau, không thay đổi giá đã chụp tại `InvoiceDetail`.
- Nhân viên phục vụ/thu ngân chỉ xem món còn bán trong nhóm đang hoạt động; Bếp và quản lý có thể xem món hết để bật lại.
- Khu vực và số bàn duy nhất; sức chứa bàn từ 1 đến 100.
- Không xóa/ngừng bàn hoặc thay đổi thông tin bàn khi bàn đang được gắn với hóa đơn chưa giải phóng.
- Xóa khu vực bị chặn khi còn bàn chưa xóa mềm trong khu vực đó.

### Quy tắc gọi món và bếp

- Mỗi bàn chỉ gắn với một hóa đơn đang phục vụ; mở bàn kiểm tra giao dịch ở mức serializable và sức chứa.
- Hóa đơn chụp ca đang hoạt động theo giờ địa phương lúc mở bàn để phục vụ báo cáo khách theo ca; ngoài khung giờ ca, `ShiftId` để trống.
- Chỉ món còn bán trong nhóm đang hoạt động được thêm vào hóa đơn; `UnitPrice` chụp giá lúc gọi món.
- Trạng thái món: `0` chờ → `1` đang chế biến → `2` hoàn thành; chỉ trạng thái `0` được hủy/đổi.
- Đổi món ghi nhận món cũ đã hủy và thêm món thay thế trong cùng giao dịch; lý do được lưu.
- Hàng đợi bếp lấy từ `vw_KitchenQueue`, sắp theo `OrderedAt` rồi `InvoiceDetailId`.
- Chuyển bàn yêu cầu bàn đích trống; gộp yêu cầu hai hóa đơn đang mở và kiểm tra tổng sức chứa.
- Mỗi hóa đơn chỉ ghi nhận một thanh toán; `Payment.Amount` là tạm tính trừ giảm giá, không lưu tiền khách đưa hoặc tiền thối.
- Chỉ MANAGER được nhập giảm giá; CASHIER chỉ thanh toán theo tổng đã tính. Món chờ/đang chế biến yêu cầu xác nhận rõ ràng trước khi đóng hóa đơn.
- Thanh toán thành công đóng hóa đơn và giải phóng toàn bộ bàn liên kết trong cùng transaction.
- Báo cáo doanh thu/khách/món chỉ tính hóa đơn đã thanh toán; công/lương chỉ tính attendance đã duyệt. Báo cáo sử dụng các view `vw_RevenueByPeriod`, `vw_CustomerBy*`, `vw_ItemSales`, `vw_ApprovedShiftCount` và `vw_Payroll`.

## Ghi chú kỹ thuật

| Nội dung | Ghi chú |
|----------|---------|
| Target framework | **net10.0** — máy phát triển hiện chỉ cài .NET SDK 10 (không có template/runtime ASP.NET Core 9). Tài liệu gốc ghi ASP.NET Core 9; nếu bắt buộc 9, đổi `TargetFramework` ở 4 `.csproj` và hạ version package EF xuống 9.x. |
| Entity | Scaffold từ database bằng `dotnet ef dbcontext scaffold` (database-first) → luôn khớp schema thật |
| View DB | 10 view được scaffold thành entity keyless, dùng cho báo cáo |
| Migration | **Chưa dùng** vì CSDL quản lý bằng script SQL trong `database/`. Nếu chuyển sang code-first, cần baseline migration đầu tiên. |
| Xác thực | **Cookie Authentication** + `PasswordHasher<AppUser>` (ASP.NET Identity, PBKDF2-HMAC-SHA512) trên bảng `AppUser`/`Role`; không dùng bảng `AspNet*`. Xem `docs/14_ImplementationLog_2026-10-27_Phase3.md`. |
| Phiên đăng nhập | `OnValidatePrincipal` kiểm tra lại DB mỗi request: tài khoản bị khóa/xóa, nhân viên nghỉ, đổi vai trò hoặc đổi security stamp (`AppUser.UpdatedAt`) → từ chối cookie cũ. Xem `docs/17`. |
| Phân quyền | Policy `Management` (ADMIN/OWNER/MANAGER) cho báo cáo; `Cashier`, `Kitchen`, `PaymentManagement` (MANAGER/CASHIER) khai báo trong `Program.cs` |
| Audit | Trigger DB ghi `AuditLog` cho tài khoản, nhân viên, nhóm món, món, ca, lịch, chấm công, hóa đơn, chi tiết món và thanh toán; người thao tác lấy từ `SESSION_CONTEXT`. Có trang tra cứu `/Audit` cho ADMIN/OWNER/MANAGER, kèm lịch sử chuyển/gộp bàn. PasswordHash không được ghi vào audit. |
| Thời gian | Mốc thời gian lưu **UTC** (`SYSUTCDATETIME()`/`DateTime.UtcNow`), hiển thị giờ địa phương. `Schedule.WorkDate` và "hôm nay" của chấm công dùng **ngày địa phương** của server. |
| Package bảo mật | Ghim `System.Security.Cryptography.Pkcs` 10.0.14 để xử lý cảnh báo NU1902/NU1903 từ `Microsoft.Data.SqlClient` |
