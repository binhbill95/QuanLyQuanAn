# Nhật ký triển khai — Bộ lọc chi nhánh cho ADMIN (2026-11-15)

## 1. Mục tiêu

ADMIN có `BranchId = NULL` nên hiện xem tổng hợp toàn bộ chi nhánh. Bổ sung bộ lọc chi nhánh để ADMIN xem riêng từng chi nhánh, **không thay đổi quyền vận hành**:

- ADMIN vẫn không mở bàn, gọi món, thanh toán hay chấm công.
- MANAGER/WAITER/CASHIER vẫn bị giới hạn theo chi nhánh của phiên.
- Tham số lọc trên URL không thể vượt qua query filter của chi nhánh.

## 2. Phạm vi thay đổi

| Màn hình | Tham số | Cách áp dụng |
|----------|---------|--------------|
| Nhân viên | `branchId` | `EmployeeSearchRequest.BranchId` + dropdown chỉ hiển thị cho ADMIN |
| Bàn ăn | `branchId` | `ITableManagementService.GetTablesAsync(branchId)` + dropdown cho ADMIN |
| Tài khoản | `branchId` | `IAccountService.SearchAsync(..., branchId)` + dropdown (màn hình chỉ ADMIN) |
| Báo cáo | `BranchId` | `ReportFilter.BranchId`; áp dụng cho cả trang xem và file CSV |

Quy tắc an toàn:

- Controller chỉ truyền `branchId` xuống service khi `User.IsInRole("ADMIN")`.
- Query filter theo chi nhánh trong `RestaurantDbContext` vẫn bật, nên MANAGER/CASHIER/WAITER gửi `branchId` khác cũng chỉ nhận dữ liệu chi nhánh mình.
- `ReportingService` dùng `effectiveBranchId = currentUser.BranchId ?? filter.BranchId`: tài khoản chi nhánh luôn thắng tham số lọc.

## 3. Thay đổi mã nguồn

**Application**

- `Models/Employees/EmployeeDtos.cs`: thêm `BranchId` vào `EmployeeSearchRequest`.
- `Models/Reports/ReportModels.cs`: thêm `BranchId` vào `ReportFilter`.
- `Abstractions/ITableManagementService.cs`: `GetTablesAsync(int? branchId, CancellationToken)`.
- `Abstractions/IAccountService.cs`: `SearchAsync(..., int? branchId, CancellationToken)`.

**Infrastructure**

- `Employees/EmployeeService.cs`: lọc `e.BranchId == request.BranchId` khi có tham số.
- `Tables/TableManagementService.cs`: lọc bàn theo chi nhánh trước khi sắp xếp.
- `Accounts/AccountService.cs`: lọc tài khoản theo `BranchId`.
- `Reporting/ReportingService.cs`: áp dụng chi nhánh hiệu lực cho cả 6 tập dữ liệu báo cáo.

**Web**

- `Models/EmployeeFormViewModel.cs`, `MenuViewModels.cs`, `AccountViewModels.cs`, `ReportsViewModels.cs`: thêm `BranchId`, danh sách chi nhánh và cờ `ShowBranchFilter`.
- `Controllers/EmployeesController.cs`, `TablesController.cs`, `AccountsController.cs`, `ReportsController.cs`: nhận `branchId`, nạp `IBranchService.GetOptionsAsync` cho ADMIN.
- `Views/Employees/Index.cshtml`, `Tables/Index.cshtml`, `Accounts/Index.cshtml`, `Reports/Index.cshtml`: thêm dropdown "Tất cả chi nhánh"; giữ `branchId` khi phân trang và khi Xuất CSV.

## 4. Dữ liệu seed cho CN002

Bổ sung vào `database/02_seed.sql` (idempotent):

| Loại | Giá trị |
|------|---------|
| Nhân viên | `NV901` — Nguyễn Test Chi Nhánh 2 (phục vụ, CN002) |
| Nhân viên | `NV902` — Trần Thu Ngân Chi Nhánh 2 (tiếp tân, CN002) |
| Tài khoản | `nv901` / `Admin@123` — WAITER, gắn NV901 |
| Tài khoản | `thungan2` / `Admin@123` — CASHIER, gắn NV902 |
| Bàn | `B11` — CN002 (đã có từ trước) |

Nhờ vậy database khởi tạo mới có thể demo ngay luồng CN002 mà không cần tạo tay.

## 5. Kết quả kiểm thử (2026-11-15)

Build: `0 warning / 0 error`. `dotnet test`: 11 skipped, 0 failed (chưa cấu hình connection string tích hợp).

Kiểm thử thủ công trên web:

| Kịch bản | Kết quả |
|----------|---------|
| ADMIN – Nhân viên, tất cả chi nhánh | NV001–NV005, NV901, NV902 |
| ADMIN – Nhân viên, `branchId=2` | Chỉ NV901, NV902 |
| ADMIN – Bàn ăn, `branchId=2` | Chỉ B11 |
| ADMIN – Tài khoản, `branchId=1` | manager, sale, thungan, bep |
| ADMIN – Tài khoản, `branchId=2` | nv901, thungan2 |
| ADMIN – Báo cáo, `BranchId=2` | Doanh thu 175,000 đ, bàn B11, nhân viên NV901 |
| ADMIN – Xuất CSV, `BranchId=2` | Chỉ dữ liệu CN002 |
| MANAGER CN001 gửi `branchId=2` | Vẫn chỉ thấy nhân viên CN001; không có dropdown lọc |
| CASHIER CN002 gửi `branchId=1` | Vẫn chỉ thấy bàn B11 |

Luồng CN002 đầu-cuối:

1. `nv901` mở bàn B11, gọi món.
2. `thungan2` chỉ thấy hóa đơn CN002, thanh toán tiền mặt 175,000 đ.
3. Hóa đơn đóng, bàn B11 được giải phóng.
4. `thungan2` không thấy và không thanh toán được hóa đơn CN001 (HTTP 404 khi mở Checkout).

## 6. Hạn chế còn lại

- Chưa có branch switcher toàn cục trên thanh điều hướng; bộ lọc đang đặt tại từng màn hình.
- `AuditLog` chưa có `BranchId` nên chưa lọc audit theo chi nhánh.
- Thực đơn/giá và khu vực bàn vẫn dùng chung toàn hệ thống.
- `vw_KitchenQueue` chưa có cột `BranchId` (đang lọc gián tiếp qua hóa đơn).
