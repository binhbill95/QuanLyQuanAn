# Implementation Log — Phase 4: Quản lý nhân viên (2026-10-27)

> Nối tiếp `14_ImplementationLog_2026-10-27_Phase3.md` (xác thực & phân quyền).
> Phiên này: **CRUD hồ sơ nhân viên**, tìm kiếm/lọc/phân trang, xóa mềm, kiểm tra nghiệp vụ ở tầng service.
> Phase 4b (ca làm & chấm công) hoàn thành ở `16_ImplementationLog_2026-10-27_Phase4b.md`.

## 1. Thông tin phiên

| Hạng mục | Nội dung |
|----------|----------|
| Ngày | 2026-10-27 |
| Phạm vi | Phase 4 — Employee Management (hồ sơ nhân viên) |
| Kết quả | Build **0 warning / 0 error**; 19/19 kịch bản kiểm thử đạt |
| Môi trường | .NET 10, EF Core 10, SQL Server 2022 (container healthy) |
| Thời lượng | ~50 phút |

## 2. Mục tiêu & kết quả

| # | Yêu cầu | Kết quả |
|---|---------|---------|
| 1 | Danh sách nhân viên + tìm kiếm + lọc + phân trang | ✅ |
| 2 | Thêm nhân viên (gợi ý mã tự động) | ✅ |
| 3 | Sửa hồ sơ | ✅ |
| 4 | Xem chi tiết | ✅ |
| 5 | Xóa mềm (giữ dữ liệu lịch sử) | ✅ |
| 6 | Kiểm tra trùng mã / trùng CCCD | ✅ |
| 7 | Quy tắc nghỉ việc: trạng thái “Đã nghỉ” phải có ngày nghỉ | ✅ |
| 8 | Chỉ ADMIN/OWNER/MANAGER được truy cập | ✅ |
| 9 | Khóa tài khoản khi nhân viên nghỉ / bị xóa | ✅ |
| 10 | Ghi nhận người tạo/cập nhật (`CreatedBy`/`UpdatedBy`) | ✅ |

## 3. Kiến trúc

```
EmployeesController  [Authorize(Policy="Management")]
   │  EmployeeListViewModel / EmployeeFormViewModel (DataAnnotations)
   ▼
IEmployeeService (Application)
   ├─ SearchAsync         → PagedResult<EmployeeListItemDto>
   ├─ GetDetailsAsync     → EmployeeDetailsDto?
   ├─ GetInputAsync       → EmployeeInput? (form sửa)
   ├─ CreateAsync         → OperationResult<int>
   ├─ UpdateAsync         → OperationResult<bool>
   ├─ SoftDeleteAsync     → OperationResult<bool>
   ├─ GetPositionsAsync   → dropdown vị trí
   └─ SuggestEmployeeCodeAsync → NV005…
   ▼
EmployeeService (Infrastructure)
   ├─ EF Core: filter + phân trang phía SQL (Skip/Take)
   ├─ ValidateAsync: quy tắc nghiệp vụ + unique
   └─ ICurrentUserService → CreatedBy/UpdatedBy
```

### Quy tắc nghiệp vụ đã cài

| # | Quy tắc | Xử lý |
|---|---------|-------|
| 1 | Mã nhân viên duy nhất | Kiểm tra **toàn bộ bảng** (kể cả bản ghi đã xóa mềm, vì unique index của DB áp dụng cả bản ghi xóa mềm) |
| 2 | CCCD/CMND duy nhất | Như trên |
| 3 | `Status = 2` (Đã nghỉ) ⇒ phải có `EndDate` | Chặn ở service + DB CHECK |
| 4 | `EndDate >= StartDate` | Chặn ở service + DB CHECK |
| 5 | Ngày sinh không ở tương lai, năm ≥ 1900 | Chặn ở service |
| 6 | Vị trí phải tồn tại và chưa xóa | Kiểm tra trước khi lưu |
| 7 | Xóa = xóa mềm (`IsDeleted`, `DeletedAt`) | Không xóa vật lý |
| 8 | Nhân viên nghỉ việc/bị xóa ⇒ khóa tài khoản | `AppUser.IsActive = 0` |
| 9 | Không cho xóa nhân viên đang đăng nhập | Chặn ở service |
| 10 | Mã tự động | Lấy số lớn nhất của `NV###` + 1 |

> `Employee.ShiftRate` để trống = dùng `Position.DefaultShiftRate` (đúng thiết kế 3NF ở `docs/11` mục 5.2.2).

## 4. Kết quả kiểm chứng

Chạy app tại `http://localhost:5010`, đăng nhập `manager` / `Admin@123`.

| # | Kịch bản | Kỳ vọng | Thực tế | Kết quả |
|---|----------|---------|---------|:-------:|
| 1 | `GET /Employees` | 200, 4 nhân viên seed | 200, "Tổng cộng 4 nhân viên" | ✅ |
| 2 | `GET /Employees/Create` | Gợi ý mã kế tiếp | `NV005` | ✅ |
| 3 | Tạo NV005 | Chuyển trang chi tiết + thông báo | `/Employees/Details/5` + "Đã thêm nhân viên" | ✅ |
| 4 | Tạo trùng mã NV005 | Báo lỗi | "Mã nhân viên 'NV005' đã tồn tại." | ✅ |
| 5 | Tạo trùng CCCD (của NV005 đã xóa mềm) | Báo lỗi | "…đã được dùng cho nhân viên khác." | ✅ |
| 6 | Số điện thoại sai (`12345`) | Báo lỗi | "Số điện thoại không hợp lệ…" | ✅ |
| 7 | Tìm `keyword=NV002` | Chỉ còn 1 kết quả | "Tổng cộng 1 nhân viên" | ✅ |
| 8 | Lọc `positionId=1` (Phục vụ) | 2 nhân viên | "Tổng cộng 2 nhân viên" | ✅ |
| 9 | Lọc `status=1` | Không có ai | "Không tìm thấy nhân viên nào." | ✅ |
| 10 | Chi tiết NV001 | Hiện vị trí + tài khoản `manager` | đúng | ✅ |
| 11 | Sửa NV005 → `Status=2`, bỏ trống ngày nghỉ | Báo lỗi | "Nhân viên đã nghỉ phải có ngày nghỉ." | ✅ |
| 12 | Sửa NV005 → `Status=2`, `EndDate=2026-10-20` | Lưu thành công | Chi tiết hiện badge "Đã nghỉ" | ✅ |
| 13 | Xóa NV005 | Xóa mềm, danh sách còn 4 | đúng, "Đã xóa nhân viên" | ✅ |
| 14 | Xóa nhân viên đang đăng nhập (NV001) | Bị chặn | "Không thể xóa nhân viên đang đăng nhập." | ✅ |
| 15 | Waiter gọi `/Employees` | 302 → AccessDenied | 302 → `/Account/AccessDenied?ReturnUrl=%2FEmployees` | ✅ |
| 16 | Waiter xem trang chủ | Không thấy menu/card nhân viên | không thấy | ✅ |
| 17 | CSDL sau xóa | `IsDeleted=1`, `UpdatedBy=2` | đúng (`DeletedAt 2026-10-27 04:06:02`) | ✅ |
| 18 | Audit trigger | Có bản ghi INSERT/UPDATE/DELETE | 3 bản ghi `AuditLog` cho `Employee` | ✅ |
| 19 | Build | 0 warning / 0 error | đúng | ✅ |

**Dọn dữ liệu test:** đã xóa vật lý NV005 + 3 dòng audit test → CSDL trở về 4 nhân viên seed, 2 tài khoản.

### Hạn chế đã biết

`AuditLog.ChangedByUserId` tại thời điểm kiểm thử Phase 4 vẫn **NULL** — trigger khi đó chỉ ghi thời điểm và dữ liệu thay đổi. Đã khắc phục trong `docs/22_ImplementationLog_2026-10-28_Audit.md` bằng `SESSION_CONTEXT` và giao diện tra cứu.

1. Thêm `SESSION_CONTEXT(N'UserId')` trong trigger (`database/06_audit_user.sql`).
2. Từ ứng dụng: mở connection, chạy `sp_set_session_context` trước khi `SaveChanges` (dùng `DbConnectionInterceptor`).
3. Ghi vào `docs/12` sau khi làm.

## 5. Quyết định kỹ thuật

| # | Quyết định | Lý do | Hệ quả |
|---|-----------|-------|--------|
| 1 | Tách `EmployeeFormViewModel` (Web) khỏi `EmployeeInput` (Application) | View có DataAnnotations tiếng Việt; service không phụ thuộc MVC | Mapping thủ công 2 chiều, đổi lại tách bạch tầng |
| 2 | Kiểm tra nghiệp vụ trong service, không chỉ dựa vào CHECK constraint | DB trả lỗi kỹ thuật khó hiểu; cần thông báo tiếng Việt | Có kiểm tra trùng lặp; CHECK vẫn là lớp bảo vệ cuối |
| 3 | Kiểm tra unique **cả bản ghi xóa mềm** | Unique index của SQL Server không loại trừ `IsDeleted` | Mã/CCCD của nhân viên đã xóa không tái sử dụng được (đúng nghiệp vụ lưu trữ) |
| 4 | Xóa mềm + khóa tài khoản | Bảo toàn hóa đơn/chấm công/báo cáo; chặn đăng nhập | Không thể xóa cứng từ UI |
| 5 | Phân trang + lọc phía SQL (`Skip/Take`) | Không tải toàn bộ bảng | Hiệu năng tốt khi dữ liệu lớn |
| 6 | `PagedResult` giữ `PageSize` cố định 20 | Đơn giản cho đồ án | Có thể truyền `pageSize` khi cần |
| 7 | Không xử lý `RowVersion` (optimistic concurrency) | Ưu tiên hoàn thành CRUD; 1 người sửa tại một thời điểm | Ghi TODO — xem mục 8 |
| 8 | `EmployeeService` phụ thuộc `ICurrentUserService` | Lấy `UserId` cho audit fields | Interface ở Application, implement ở Web → DI phải đăng ký ở Web (đã có) |

## 6. Lỗi gặp phải & cách xử lý

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|-----|-------------|-----------|
| 1 | Test đọc DB thấy `IsDeleted=0` dù UI đã xóa | Chạy song song 2 lệnh: query SQL chạy **trước** khi POST delete hoàn tất | Chạy lại query sau khi request kết thúc; ghi chú để không lặp lại |
| 2 | `$home` báo "read-only or constant" | `$HOME` là biến dành riêng của PowerShell | Đổi tên biến test thành `$homePage` |
| 3 | So khớp chuỗi tiếng Việt trong HTML bị sai | Razor HTML-encode nội dung động | `[System.Net.WebUtility]::HtmlDecode()` trước khi assert (đã dùng từ Phase 3) |
| 4 | `asp-for` với `DateOnly` có thể render sai định dạng cho `<input type="date">` | Mặc định format theo culture | Thêm `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]` |
| 5 | `EmployeeService` không resolve được khi chạy | `ICurrentUserService` chỉ đăng ký ở Web | Giữ nguyên kiến trúc (Application interface + Web implementation); ghi chú trong README |

## 7. Tệp đã tạo / cập nhật

| Tệp | Trạng thái | Ghi chú |
|-----|-----------|---------|
| `Application/Common/OperationResult.cs` | Mới | Kết quả thao tác ghi |
| `Application/Models/Employees/EmployeeDtos.cs` | Mới | 5 DTO/record |
| `Application/Abstractions/IEmployeeService.cs` | Mới | Hợp đồng nghiệp vụ |
| `Infrastructure/Employees/EmployeeService.cs` | Mới | ~330 dòng, toàn bộ quy tắc |
| `Infrastructure/DependencyInjection.cs` | Cập nhật | Đăng ký `IEmployeeService` |
| `Web/Controllers/EmployeesController.cs` | Mới | Index/Details/Create/Edit/Delete |
| `Web/Models/EmployeeFormViewModel.cs` | Mới | DataAnnotations tiếng Việt + options |
| `Web/Models/EmployeeIndexViewModel.cs` | Mới | (trong cùng file trên) danh sách + bộ lọc |
| `Web/Helpers/EmployeeDisplay.cs` | Mới | Nhãn/badge trạng thái, giới tính |
| `Web/Views/Employees/Index.cshtml` | Mới | Bảng + lọc + phân trang |
| `Web/Views/Employees/Details.cshtml` | Mới | Chi tiết + audit timestamps |
| `Web/Views/Employees/Create.cshtml`, `Edit.cshtml` | Mới | Form |
| `Web/Views/Employees/_FormFields.cshtml` | Mới | Partial dùng chung |
| `Web/Views/_ViewImports.cshtml` | Cập nhật | Thêm namespace Application/Helpers |
| `Web/Views/Shared/_Layout.cshtml` | Cập nhật | Menu "Nhân viên" cho Management |
| `Web/Views/Home/Index.cshtml` | Cập nhật | Card nhân viên → liên kết thật |
| `docs/09_DevelopmentRoadmap.md` | Cập nhật | Phase 4 ✅ |
| `docs/13`, `docs/14` | Cập nhật | Liên kết nhật ký |
| `src/README.md` | Cập nhật | Mô tả chức năng mới |

## 8. Việc còn lại

| # | Việc | Ưu tiên |
|---|------|:-------:|
| 1 | ~~**Phase 4b**: ca làm (`Shift`), ca của nhân viên (`EmployeeShift`), xếp ca (`Schedule`), chấm công cơ bản (`Attendance`)~~ → ✅ đã làm, xem `16_ImplementationLog_2026-10-27_Phase4b.md` | Cao |
| 2 | Gắn `ChangedByUserId` cho AuditLog (SESSION_CONTEXT + trigger) | Trung bình |
| 3 | ~~Quản lý tài khoản: tạo user cho nhân viên, reset mật khẩu, khóa/mở~~ → ✅ đã làm, xem `17_ImplementationLog_2026-10-27_Phase4c.md` | Cao |
| 4 | Xử lý `RowVersion` (optimistic concurrency) cho form sửa | Thấp |
| 5 | Unit test `EmployeeService` (unique, ngày nghỉ, xóa mềm) | Trung bình |
| 6 | Xuất Excel danh sách nhân viên | Thấp |
| 7 | Ảnh đại diện nhân viên | Thấp |
| 8 | Kiểm tra quyền chi tiết theo `RolePermission` (hiện dùng policy theo role) | Thấp |

## 9. Cách chạy

```bash
cd docker && docker compose up -d
cd ../src && dotnet run --project RestaurantManagement.Web
# http://localhost:5010 → manager / Admin@123 → menu "Nhân viên"
```

## 10. Ghi chú cập nhật tài liệu

1. Thêm trường nhân viên → sửa `docs/11` (bảng `Employee`), entity, DTO, form, service, migration/script.
2. Đổi quy tắc nghiệp vụ → sửa mục 3 file này + `docs/02` (business rules).
3. Mọi kịch bản kiểm thử mới → bổ sung mục 4.
4. Dữ liệu test phải được dọn sạch trước khi kết thúc phiên (như NV005 ở phiên này).
