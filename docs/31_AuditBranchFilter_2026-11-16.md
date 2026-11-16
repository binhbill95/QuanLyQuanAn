# Nhật ký triển khai — Lọc nhật ký thao tác theo chi nhánh (2026-11-16)

## 1. Mục tiêu

Trang `/Audit` trước đây chỉ lọc theo ngày, thao tác và từ khóa; `AuditLog` chưa có cột chi nhánh nên MANAGER thấy lẫn thay đổi của chi nhánh khác và ADMIN không thể xem riêng từng chi nhánh.

Bổ sung:

- `AuditLog.BranchId` (nullable) + khóa ngoại + index.
- Trigger audit tự gắn chi nhánh theo bảng gốc.
- Bộ lọc chi nhánh cho ADMIN trên `/Audit` và cột "Chi nhánh" trong bảng kết quả.
- Query filter thứ 14 trong `RestaurantDbContext`, bảo vệ tài khoản chi nhánh.

Không thay đổi quyền: ADMIN vẫn không mở bàn/thanh toán/chấm công.

## 2. Cách xác định chi nhánh cho từng loại nhật ký

| Bảng được audit | Nguồn `BranchId` |
|---|---|
| `Employee` | `COALESCE(inserted.BranchId, deleted.BranchId)` |
| `Invoice` | `COALESCE(inserted.BranchId, deleted.BranchId)` |
| `AppUser` | `COALESCE(inserted.BranchId, deleted.BranchId)` |
| `InvoiceDetail` | `Invoice.BranchId` theo `InvoiceId` |
| `Payment` | `Invoice.BranchId` theo `InvoiceId` |
| `Schedule` | `Employee.BranchId` theo `EmployeeId` |
| `Attendance` | `Schedule → Employee.BranchId` |
| `Category`, `MenuItem`, `Shift` | `NULL` — dữ liệu dùng chung toàn hệ thống |

Quy tắc hiển thị:

- ADMIN (`BranchId = NULL`): thấy tất cả; có dropdown "Tất cả chi nhánh" hoặc chọn từng chi nhánh.
- MANAGER/WAITER/CASHIER: query filter chỉ trả bản ghi đúng chi nhánh của phiên; tham số `BranchId` trên URL bị bỏ qua.
- Bản ghi `BranchId = NULL` (thực đơn, danh mục, ca làm, tài khoản ADMIN) chỉ ADMIN thấy; giao diện ghi "Dùng chung".

## 3. Thay đổi cơ sở dữ liệu

**`database/01_schema.sql`**

- `AuditLog.BranchId int NULL`.
- `FK_AuditLog_Branch` → `Branch(Id)`.
- `IX_AuditLog_Branch_ChangedAt (BranchId, ChangedAt)`.

**`database/04_audit.sql`**

- Cả 10 trigger thêm cột `BranchId` khi ghi `AuditLog`.
- AppUser audit thêm `BranchId` vào JSON `OldValues/NewValues` và theo dõi cả thay đổi `BranchId`.

**`database/06_branch_management.sql`** (idempotent, cho database cũ)

- Thêm cột nếu chưa có.
- Backfill:
  - `Invoice`, `Employee`, `AppUser`: đọc `$.BranchId` từ JSON.
  - `InvoiceDetail`, `Payment`: `$.InvoiceId` → `Invoice.BranchId`.
  - `Schedule`: `$.EmployeeId` → `Employee.BranchId`.
  - `Attendance`: `$.ScheduleId` → `Schedule.EmployeeId` → `Employee.BranchId`.
  - Fallback cho bản ghi cũ thiếu JSON: đối chiếu bảng gốc qua `RecordId`; hóa đơn đã xóa cứng thì lấy từ nhật ký của chính hóa đơn đó.
- Thêm FK + index nếu chưa có.

Với database cũ: chạy `06_branch_management.sql` rồi chạy lại `04_audit.sql` để trigger mới gắn chi nhánh.

## 4. Thay đổi ứng dụng

**Domain / Infrastructure**

- `Domain/Entities/AuditLog.cs`: thêm `BranchId`, navigation `Branch`.
- `Persistence/RestaurantDbContext.cs`: cấu hình cột, FK, index và query filter:
  `CurrentBranchId == null || e.BranchId == CurrentBranchId`.
- `Auditing/AuditService.cs`: lọc `AuditLog.BranchId` và `TableTransferLog.Invoice.BranchId`; trả thêm tên chi nhánh trong DTO.

**Application**

- `Models/Audit/AuditActivityModels.cs`: `AuditActivityFilter.BranchId`, `AuditActivityDto.BranchName`.

**Web**

- `Models/AuditViewModels.cs`: `BranchId`, `Branches`, `ShowBranchFilter`.
- `Controllers/AuditController.cs`: chỉ truyền `BranchId` khi `User.IsInRole("ADMIN")`; nạp danh sách chi nhánh cho ADMIN.
- `Views/Audit/Index.cshtml`: dropdown "Tất cả chi nhánh", cột "Chi nhánh", giữ `BranchId` khi phân trang.

## 5. Kết quả kiểm thử (2026-11-16)

Build: `0 warning / 0 error`.

| Kịch bản | Kết quả |
|---|---|
| ADMIN `/Audit` (tất cả) | 115 bản ghi |
| ADMIN `/Audit?BranchId=1` | 93 bản ghi, có hóa đơn CN001 `5019` |
| ADMIN `/Audit?BranchId=2` | 21 bản ghi, có hóa đơn CN002 `5018`, nhân viên `NV901`; không có `5019` |
| ADMIN — cột Chi nhánh | Hiển thị đúng "Chi nhánh chính"/"Chi nhánh 2"/"Dùng chung" |
| MANAGER `/Audit` | 93 bản ghi (chỉ CN001); không có dropdown lọc |
| MANAGER `/Audit?BranchId=2` | Vẫn 93 bản ghi CN001; không có dòng "Chi nhánh 2" |
| SQL cập nhật `Employee` NV901 | Sinh `AuditLog` mới với `BranchId = 2`, actor `admin` (id 1) |
| ADMIN `/Audit?BranchId=2` sau cập nhật | Thấy bản ghi mới; MANAGER không thấy |
| `/health/db` | `200 {"status":"healthy"}` |

Tổng 115 = 93 (CN001) + 21 (CN002) + 1 bản ghi dùng chung (`Category`) — bản ghi dùng chung nằm ngoài trang đầu nên không xuất hiện trong ảnh chụp màn hình kiểm thử.

## 6. Hạn chế còn lại

- Chưa có trigger audit cho bảng `Branch` (tạo/sửa chi nhánh không vào `AuditLog`).
- Vài bản ghi lịch sử rất cũ không còn bảng gốc lẫn JSON chứa chi nhánh sẽ giữ `BranchId = NULL` và chỉ ADMIN thấy.
- `vw_KitchenQueue` vẫn chưa có cột `BranchId` (lọc gián tiếp qua hóa đơn).
- Chưa có branch switcher toàn cục; bộ lọc đặt tại từng màn hình.
