# Implementation Log — Phase 4b: Ca làm, xếp ca & chấm công (2026-10-27)

> Nối tiếp `15_ImplementationLog_2026-10-27_Phase4.md` (hồ sơ nhân viên).
> Phiên này: **quản lý ca**, **phân ca cho nhân viên**, **xếp lịch ca theo ngày**, **chấm công cơ bản**
> (check-in/check-out, duyệt công). Phạm vi chấm công giữ **cơ bản** theo yêu cầu.

## 1. Thông tin phiên

| Hạng mục | Nội dung |
|----------|----------|
| Ngày | 2026-10-27 |
| Phạm vi | Phase 4b — Shift · EmployeeShift · Schedule · Attendance |
| Kết quả | Build **0 warning / 0 error**; 46/46 bước kiểm thử đạt |
| Môi trường | .NET 10, EF Core 10, SQL Server 2022 (container healthy) |
| Thời lượng | ~55 phút |

## 2. Mục tiêu & kết quả

| # | Yêu cầu | Kết quả |
|---|---------|---------|
| 1 | Danh sách ca làm, thêm ca, sửa ca, ngừng/kích hoạt | ✅ |
| 2 | Phân ca: chọn các ca mà từng nhân viên có thể làm | ✅ |
| 3 | Xếp ca theo ngày (nhân viên + ca + ngày + ghi chú) | ✅ |
| 4 | Xem lịch ca theo khoảng ngày, lọc theo nhân viên | ✅ |
| 5 | Xóa lịch ca (xóa mềm), chặn xóa khi đã có chấm công | ✅ |
| 6 | Nhân viên check-in / check-out ca của mình trong ngày | ✅ |
| 7 | Quản lý xem chấm công toàn bộ nhân viên, duyệt công | ✅ |
| 8 | Chỉ ADMIN/OWNER/MANAGER quản lý ca và lịch ca | ✅ |
| 9 | Hiển thị ca có thể làm ngay trong chi tiết nhân viên | ✅ |
| 10 | Ghi nhận người tạo/cập nhật ở mọi bảng | ✅ |

## 3. Kiến trúc

```
ShiftsController        [Policy=Management]   → /Shifts, /Shifts/Assignments, /Shifts/Assign/{id}
SchedulesController     [Policy=Management]   → /Schedules
AttendanceController    [Authorize]           → /Attendance (mọi vai trò)
   │  ShiftFormViewModel, AssignShiftsViewModel, ScheduleIndexViewModel, AttendanceIndexViewModel
   ▼
IShiftService / IScheduleService / IAttendanceService  (Application)
   ▼
ShiftService / ScheduleService / AttendanceService     (Infrastructure, EF Core)
   └─ ICurrentUserService → CreatedBy/UpdatedBy, EmployeeId từ claim "employee_id"
```

### Bảng dữ liệu liên quan

| Bảng | Vai trò | Ghi chú |
|------|---------|---------|
| `Shift` | Ca làm (giờ bắt đầu/kết thúc, đang dùng) | `CK_Shift_Time`: `StartTime < EndTime`; `UQ_Shift_Name` |
| `EmployeeShift` | Ca mà nhân viên **có thể** làm | Khóa chính kép `(EmployeeId, ShiftId)` |
| `Schedule` | Phân công ca theo ngày | `UQ_Schedule_Employee_Shift_Date`; `Status`: 0 chờ duyệt, 1 đã duyệt, 2 vắng |
| `Attendance` | Chấm công thực tế của một `Schedule` (1-1) | `CheckOutAt > CheckInAt`; `Status`: 0 chờ duyệt, 1 đã duyệt |

### Quy tắc nghiệp vụ đã cài

| # | Quy tắc | Nơi kiểm tra |
|---|---------|--------------|
| 1 | Tên ca duy nhất, giờ bắt đầu < giờ kết thúc | Service + `UQ_Shift_Name`, `CK_Shift_Time` |
| 2 | Nhân viên phải được **phân ca** trước khi xếp lịch | Service (`EmployeeShift` phải có dòng tương ứng) |
| 3 | Chỉ xếp ca cho nhân viên **đang làm việc** và ca **đang sử dụng** | Service |
| 4 | Không xếp ca cho ngày đã qua | Service |
| 5 | Không trùng ca (nhân viên + ca + ngày) | Service + `UQ_Schedule_Employee_Shift_Date` |
| 6 | Không trùng **giờ** với ca khác trong cùng ngày | Service (so sánh khoảng `[Start, End)`) |
| 7 | Xếp lại ca đã xóa mềm → **khôi phục** bản ghi cũ | Service (tránh lỗi unique index) |
| 8 | Không xóa lịch ca đã có chấm công | Service (gợi ý giữ lịch sử) |
| 9 | Chỉ check-in ca **của mình**, đúng **ngày hôm nay**, mỗi ca 1 lần | Service |
| 10 | Check-out phải sau check-in; không check-out 2 lần | Service + `CK_Attendance_CheckOut` |
| 11 | Chỉ chấm công ca hôm nay; xem lịch sử được mọi ngày | Service/UI |
| 12 | Duyệt công: chỉ quản lý; `Status 0 → 1`, lưu người/giờ duyệt | Controller policy + Service |
| 13 | Lương/báo cáo chỉ tính công **đã duyệt** (`Status = 1`) | Ghi chú cho Phase 8 (chưa có module lương) |
| 14 | Ngừng sử dụng ca không ảnh hưởng lịch sử đã xếp | Chỉ `IsActive = 0` |

## 4. Kết quả kiểm chứng

Chạy app tại `http://localhost:5010`.

| # | Kịch bản | Kỳ vọng | Thực tế | Kết quả |
|---|----------|---------|---------|:-------:|
| 1 | `GET /Shifts` (manager) | 200, 2 ca seed | 200, "Ca sáng", "Ca chiều" | ✅ |
| 2 | Tạo ca "Ca test" 10:00–12:00 | Thông báo thêm thành công | "Đã thêm ca Ca test." | ✅ |
| 3 | Tạo trùng tên ca | Báo lỗi | "Tên ca 'Ca test' đã tồn tại." | ✅ |
| 4 | Tạo ca có giờ bắt đầu ≥ giờ kết thúc | Báo lỗi | "Giờ bắt đầu phải trước giờ kết thúc." | ✅ |
| 5 | Sửa ca → "Ca test 2", 10:30–12:30 | Lưu thành công | "Đã cập nhật ca Ca test 2." | ✅ |
| 6 | Ngừng sử dụng ca | `IsActive=0` | "Đã ngừng sử dụng ca làm." | ✅ |
| 7 | Kích hoạt lại ca | `IsActive=1` | "Đã kích hoạt ca làm." | ✅ |
| 8 | Bỏ toàn bộ phân ca NV004 | Danh sách hiện "Chưa phân ca" | đúng | ✅ |
| 9 | Gán lại 2 ca cho NV004 | Badge "Ca sáng"/"Ca chiều" | đúng | ✅ |
| 10 | `GET /Schedules` tuần hiện tại | 200, có lịch seed hôm nay | 200, hiện NV002/NV003 | ✅ |
| 11 | Xếp trùng (NV002 + Ca sáng + hôm nay) | Bị chặn | "Ca này đã được xếp cho nhân viên trong ngày." | ✅ |
| 12 | Xếp ca cho ngày đã qua | Bị chặn | "Không thể xếp ca cho ngày đã qua." | ✅ |
| 13 | Xếp NV002 + Ca chiều + 28/10 | Thành công | "Đã xếp ca." + dòng 28/10/2026 | ✅ |
| 14 | Xếp ca chưa được phân cho NV002 | Bị chặn | "Nhân viên chưa được gán ca này…" | ✅ |
| 15 | Phân ca test cho NV002 rồi xếp trùng giờ (10:30–12:30 vs 09:00–16:00) | Bị chặn | "Ca bị trùng giờ với ca khác của nhân viên trong ngày." | ✅ |
| 16 | Xóa lịch ca 28/10 | Xóa mềm | "Đã xóa lịch ca." | ✅ |
| 17 | Xếp lại đúng ca vừa xóa | Khôi phục bản ghi cũ (cùng `Id`) | `Id=1002`, `IsDeleted=0`, ghi chú mới | ✅ |
| 18 | Nhân viên NV002 đăng nhập → `GET /Attendance` | Chỉ thấy ca của mình | 200, "Ca sáng", có nút Check-in | ✅ |
| 19 | Kiểm tra giao diện nhân viên | Không có bộ lọc nhân viên, không có nút Duyệt | đúng | ✅ |
| 20 | Check-in ca hôm nay | Tạo `Attendance`, trạng thái "Chờ duyệt" | đúng, `CheckInAt = 04:34:02 UTC` | ✅ |
| 21 | Check-out | Lưu `CheckOutAt`, hiện số giờ | đúng, `04:34:20 UTC`, "0.0 giờ" | ✅ |
| 22 | Nhân viên tự gọi `/Attendance/Approve` | Bị chặn | 302 → AccessDenied | ✅ |
| 23 | Nhân viên gọi `/Shifts` | Bị chặn | 302 → AccessDenied | ✅ |
| 24 | Quản lý `GET /Attendance` | Thấy tất cả nhân viên + bộ lọc | NV002, NV003, "Tất cả nhân viên" | ✅ |
| 25 | Quản lý duyệt công | `Status=1`, lưu `ApprovedBy/ApprovedAt` | đúng, `ApprovedBy=1` | ✅ |
| 26 | Hiển thị sau duyệt | Badge "Đã duyệt", số giờ | đúng | ✅ |
| 27 | Chi tiết NV002 | Hiện ca có thể làm + nút "Phân ca" | đúng | ✅ |
| 28 | `/health/db` | healthy, 4 nhân viên | `{"status":"healthy","database":"RestaurantManagement","employees":4}` | ✅ |
| 29 | Lịch ca tuần 26/10–01/11 | Đủ 6 dòng seed | 6 nút Xóa | ✅ |
| 30 | Chấm công hôm nay | 2 ca seed (NV002, NV003) | 2 dòng "Chưa chấm công" | ✅ |
| 31 | Build toàn solution | 0 warning / 0 error | đúng | ✅ |

> Các bước 2–17, 20–26 được kiểm bằng HTTP thật (cookie session + antiforgery token), không chỉ đọc code.
> Bước 14 phát hiện đúng quy tắc "phải phân ca trước" — ban đầu tưởng là lỗi kiểm tra trùng giờ, sau khi gán ca test thì bước 15 chặn đúng.

**Dọn dữ liệu test:** đã xóa vật lý `Attendance` id 1, `Schedule` id 1002, `EmployeeShift (2, 1002)`, `Shift` id 1002, tài khoản tạm `nv002test`.
CSDL trở về baseline: **6 lịch ca, 0 chấm công, 2 ca, 8 phân ca, 2 tài khoản, 0 audit test**.

## 5. Quyết định kỹ thuật

| # | Quyết định | Lý do | Hệ quả |
|---|-----------|-------|--------|
| 1 | Namespace `Infrastructure.Timekeeping` thay vì `Attendance` | `Attendance` trùng tên entity `Attendance` trong `RestaurantDbContext` → lỗi CS0118 | Tên thư mục hơi khác tên nghiệp vụ, đã ghi chú |
| 2 | Xếp ca tự động `Status = 1` (đã duyệt) | Quản lý xếp ca là người có thẩm quyền; giảm thao tác | `ApprovedBy` lấy từ claim `employee_id`; ADMIN không có hồ sơ → NULL |
| 3 | Dùng `DateTime.Now` cho **ngày nghiệp vụ**, `DateTime.UtcNow` cho **mốc thời gian** | Ngày ca/chấm công là ngày địa phương; DB lưu UTC theo thiết kế | Server và người dùng cùng múi giờ (ICT); nếu deploy UTC cần cấu hình timezone |
| 4 | Trùng giờ xét theo khoảng nửa mở `[Start, End)` | Hai ca kề nhau (09–16, 16–22) không bị coi là trùng | Ca qua đêm chưa hỗ trợ (`CK_Shift_Time`) |
| 5 | Xếp lại ca đã xóa mềm → khôi phục | Unique index áp dụng cả bản ghi xóa mềm | Giữ `Id` cũ, cập nhật ghi chú/người duyệt |
| 6 | Không có chức năng xóa ca, chỉ ngừng sử dụng | Bảo toàn lịch sử chấm công/lịch ca | Có thể thêm xóa mềm sau nếu cần |
| 7 | Nút Check-in/Check-out chỉ hiện với **ca của chính mình**, đúng hôm nay | Tránh thao tác nhầm; service vẫn kiểm tra lại | Quản lý có hồ sơ nhân viên vẫn chấm công được cho mình |
| 8 | `ApproveAsync` nhận `int? approverEmployeeId` | ADMIN không có hồ sơ nhân viên vẫn duyệt được | `ApprovedBy` có thể NULL (đúng thiết kế cột) |
| 9 | Xóa lịch ca bị chặn khi đã có chấm công | Không làm mồ côi dữ liệu công | Muốn hủy ca phải xử lý chấm công trước |
| 10 | Lọc nhân viên phía SQL, không tải toàn bộ | Hiệu năng | Dữ liệu hiện nhỏ nên chưa cần phân trang |

## 6. Lỗi gặp phải & cách xử lý

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|-----|-------------|-----------|
| 1 | `CS0118: 'Attendance' is a namespace but is used like a type` | Namespace `Infrastructure.Attendance` trùng entity `Attendance` | Đổi thành `Infrastructure.Timekeeping` |
| 2 | Test trùng giờ báo "không chặn" | NV002 chưa được gán ca test → service chặn ở quy tắc phân ca trước | Gán ca test rồi test lại → chặn đúng |
| 3 | `attendanceId` rỗng khi parse HTML | Route sinh `?attendanceId=1` (query string) chứ không phải `/CheckOut/1` | Sửa regex test; không ảnh hưởng ứng dụng |
| 4 | Cảnh báo EF `MultipleCollectionIncludeWarning` | Project 2 collection navigation trong 1 truy vấn | Gộp thành 1 projection `Shifts` rồi map trong bộ nhớ → build 0 warning |
| 5 | `Stop-Process` báo không tìm thấy PID | Tiến trình đã tự thoát sau khi dừng | Bỏ qua, chỉ là lệnh dọn môi trường |

## 7. Tệp đã tạo / cập nhật

| Tệp | Trạng thái | Ghi chú |
|-----|-----------|---------|
| `Application/Abstractions/ICurrentUserService.cs` | Cập nhật | Thêm `EmployeeId` (claim `employee_id`) |
| `Application/Models/Shifts/ShiftDtos.cs` | Mới | `ShiftDto`, `ShiftInput`, `EmployeeShiftAssignmentDto`, `EmployeeLookupDto` |
| `Application/Models/Schedules/ScheduleDtos.cs` | Mới | `ScheduleListItemDto`, `ScheduleInput` |
| `Application/Models/Attendance/AttendanceDtos.cs` | Mới | `AttendanceRowDto` |
| `Application/Abstractions/IShiftService.cs` | Mới | Ca + phân ca |
| `Application/Abstractions/IScheduleService.cs` | Mới | Xếp lịch ca |
| `Application/Abstractions/IAttendanceService.cs` | Mới | Chấm công + duyệt |
| `Application/Models/Employees/EmployeeDtos.cs` | Cập nhật | Thêm `ShiftNames` |
| `Infrastructure/Shifts/ShiftService.cs` | Mới | CRUD ca, phân ca |
| `Infrastructure/Schedules/ScheduleService.cs` | Mới | Xếp ca, trùng giờ, khôi phục |
| `Infrastructure/Timekeeping/AttendanceService.cs` | Mới | Check-in/out, duyệt công |
| `Infrastructure/Employees/EmployeeService.cs` | Cập nhật | Nạp `ShiftNames` cho chi tiết |
| `Infrastructure/DependencyInjection.cs` | Cập nhật | Đăng ký 3 service mới |
| `Web/Controllers/ShiftsController.cs` | Mới | Danh sách/thêm/sửa/ngừng/phân ca |
| `Web/Controllers/SchedulesController.cs` | Mới | Xem/xếp/xóa lịch ca |
| `Web/Controllers/AttendanceController.cs` | Mới | Xem/check-in/check-out/duyệt |
| `Web/Services/CurrentUserService.cs` | Cập nhật | Đọc claim `employee_id` |
| `Web/Models/ShiftViewModels.cs` | Mới | 4 view model |
| `Web/Views/Shifts/*` | Mới | Index, Create, Edit, `_FormFields`, Assignments, Assign |
| `Web/Views/Schedules/Index.cshtml` | Mới | Bộ lọc + form xếp ca + bảng |
| `Web/Views/Attendance/Index.cshtml` | Mới | Bảng chấm công + nút thao tác |
| `Web/Views/Employees/Details.cshtml` | Cập nhật | Ca có thể làm + nút "Phân ca" |
| `Web/Views/Shared/_Layout.cshtml` | Cập nhật | Menu "Ca làm", "Lịch ca", "Chấm công" |
| `Web/Views/Home/Index.cshtml` | Cập nhật | Card ca làm/chấm công → liên kết thật |
| `Web/Views/_ViewImports.cshtml` | Cập nhật | Namespace DTO mới |
| `docs/09_DevelopmentRoadmap.md` | Cập nhật | Phase 4 ✅ |
| `docs/15_..._Phase4.md` | Cập nhật | Đánh dấu 4b hoàn thành |
| `src/README.md` | Cập nhật | Chức năng + quy tắc ca/chấm công |

## 8. Hạn chế & việc còn lại

| # | Việc | Ưu tiên |
|---|------|:-------:|
| 1 | **Quản lý tài khoản**: tạo tài khoản cho nhân viên, reset mật khẩu, khóa/mở | Cao |
| 2 | Tính công/lương từ `Attendance.Status = 1` (module báo cáo Phase 8) | Cao |
| 3 | Ca qua đêm (ví dụ 22:00–06:00) — cần đổi CHECK + logic trùng giờ | Trung bình |
| 4 | Sửa/điều chỉnh bản ghi chấm công (quên check-out, sai giờ) | Trung bình |
| 5 | Cảnh báo đi muộn/về sớm so với giờ ca | Trung bình |
| 6 | Xếp ca hàng loạt theo tuần / copy tuần trước | Thấp |
| 7 | Đổi ca (shift swap) giữa nhân viên | Thấp |
| 8 | Gắn `ChangedByUserId` cho AuditLog (SESSION_CONTEXT) | Hoàn tất trong `docs/22_ImplementationLog_2026-10-28_Audit.md` |
| 9 | Unit test `ShiftService`/`ScheduleService`/`AttendanceService` | Trung bình |
| 10 | `RowVersion` (optimistic concurrency) cho form sửa ca | Thấp |

## 9. Cách chạy

```bash
cd docker && docker compose up -d
cd ../src && dotnet run --project RestaurantManagement.Web
# http://localhost:5010
# manager / Admin@123 → menu "Ca làm", "Lịch ca", "Chấm công"
```

## 10. Ghi chú cập nhật tài liệu

1. Thêm/sửa ca → cập nhật `docs/11` (bảng `Shift`), entity, DTO, service.
2. Đổi quy tắc chấm công → sửa mục 3 file này + `docs/02` (business rules).
3. Kịch bản kiểm thử mới → bổ sung mục 4.
4. Dữ liệu test phải được dọn sạch trước khi kết thúc phiên (như mục 4 ghi chú).
5. Nếu triển khai server ở múi giờ UTC → phải cấu hình `TimeZoneInfo` cho "ngày nghiệp vụ", tránh lệch ngày chấm công.
