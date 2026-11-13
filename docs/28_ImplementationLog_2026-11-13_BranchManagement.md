# Implementation Log — Quản lý chi nhánh và phân tách dữ liệu theo chi nhánh (2026-11-13)

## Phạm vi và trạng thái

Tài liệu ghi lại phần việc quản lý chi nhánh đang triển khai trên nền commit `b92cdd4`, bao gồm các lỗi phát hiện sau khi rà soát và các sửa đổi đã hoàn tất. Toàn bộ thay đổi hiện chưa được commit.

**Trạng thái:** Build thành công (`0 warning`, `0 error`). Kiểm thử tích hợp chưa chạy được trong môi trường hiện tại vì Docker daemon không hoạt động và dịch vụ `MSSQL$SQLEXPRESS` không khởi động được; 11 kiểm thử được bỏ qua (skipped), không có kiểm thử thất bại.

## 1. Lỗi build đã sửa

| Lỗi | Vị trí | Cách xử lý |
|------|--------|------------|
| 5 lỗi `CS1061` khi build | `AccountsController`, `Views/Accounts/Create.cshtml` | Bổ sung `EmployeeId`, `BranchId`, `BranchName` vào `AccountEditViewModel`; bổ sung `BranchName` vào `AccountEmployeeOptionDto`; cập nhật view dùng đúng thuộc tính. |

Kết quả: `dotnet build RestaurantManagement.slnx` thành công, không còn cảnh báo hay lỗi.

## 2. Tài khoản và chi nhánh

| Vấn đề | Cách xử lý | Tệp chính |
|--------|------------|-----------|
| Tài khoản tạo mới không xác định được chi nhánh | Chi nhánh được suy ra từ hồ sơ nhân viên được chọn, không chọn độc lập trên form Create | `AccountService.CreateAsync`, `Views/Accounts/Create.cshtml` |
| Danh sách nhân viên cấp tài khoản có thể thuộc chi nhánh đã ngừng hoạt động | `GetEmployeeOptionsAsync` loại nhân viên của chi nhánh inactive/đã xóa; hiển thị thêm tên chi nhánh trong option | `AccountService.cs`, `AccountDtos.cs` |
| Sửa tài khoản không kiểm soát chi nhánh | Tài khoản gắn nhân viên hiển thị chi nhánh chỉ đọc; tài khoản không gắn nhân viên (ví dụ `bep`) được chọn chi nhánh | `Views/Accounts/Edit.cshtml`, `AccountsController.Edit` |
| Đổi vai trò không cập nhật chi nhánh | `UpdateRoleAsync` nhận thêm `branchId`; nếu tài khoản có nhân viên thì lấy chi nhánh từ nhân viên, ngược lại dùng chi nhánh được chọn; kiểm tra chi nhánh tồn tại và đang hoạt động | `AccountService.UpdateRoleAsync`, `AccountsController` |
| Tài khoản thuộc chi nhánh bị ngừng hoạt động vẫn đăng nhập được | Đăng nhập và kiểm tra phiên đều yêu cầu chi nhánh còn hoạt động; claim `branch_id`, `branch_name` được thêm vào cookie | `UserAuthenticationService`, `AccountController`, `CurrentUserService`, `UserSessionState`, `AuthenticatedUser` |

Ghi chú: khi mã hoặc tên chi nhánh thay đổi, `BranchService.UpdateAsync` cập nhật `UpdatedAt` của các tài khoản thuộc chi nhánh để security stamp thay đổi, buộc đăng nhập lại và làm mới tên chi nhánh trên giao diện.

## 3. Báo cáo và view SQL

| Vấn đề | Cách xử lý | Tệp chính |
|--------|------------|-----------|
| Báo cáo lương không lọc được theo chi nhánh | Bổ sung `e.[BranchId]` vào `vw_Payroll`; lọc trực tiếp trên `VwPayroll.BranchId` | `database/03_views.sql`, `ReportingService.cs` |
| Bộ lọc nhân viên trong báo cáo áp dụng sai điều kiện chi nhánh | Bỏ điều kiện lọc bổ sung; danh sách nhân viên đã được lọc tự động bởi query filter toàn cục theo chi nhánh | `ReportsController.GetOptionsAsync` |
| Các view báo cáo gộp lẫn dữ liệu nhiều chi nhánh | Bổ sung `i.[BranchId]` vào select/group by của doanh thu, doanh thu theo ca, khách theo nhân viên, khách theo bàn, món bán chạy và lương | `database/03_views.sql` |
| Dịch vụ báo cáo chưa lọc theo chi nhánh của người dùng | Áp dụng `CurrentUser.BranchId` cho cả 6 nguồn dữ liệu báo cáo (doanh thu, ca, nhân viên, bàn, món, lương) | `ReportingService.GetDashboardAsync` |

## 4. Bàn, hóa đơn và kiểm tra chéo chi nhánh

| Vấn đề | Cách xử lý | Tệp chính |
|--------|------------|-----------|
| Số bàn bị ràng buộc duy nhất toàn hệ thống | Đổi ràng buộc thành duy nhất theo từng chi nhánh: `UQ_DiningTable_Branch_TableNumber (BranchId, TableNumber)` | `database/01_schema.sql`, `06_branch_management.sql`, `RestaurantDbContext.cs` |
| Kiểm tra trùng số bàn báo sai phạm vi | Chỉ kiểm tra trùng trong cùng chi nhánh; thông báo đổi thành "Số bàn '{number}' đã tồn tại trong chi nhánh." | `TableManagementService.ValidateAsync` |
| Form bàn không có chi nhánh hoặc cho đổi chi nhánh tùy ý | Bổ sung chọn chi nhánh cho ADMIN; người dùng thuộc chi nhánh chỉ thao tác trong chi nhánh của mình | `TablesController`, `Views/Tables/_TableFormFields.cshtml`, `TableManagementService` |
| Hóa đơn không lưu chi nhánh | Gán `Invoice.BranchId` theo bàn khi mở bàn; thêm cột, khóa ngoại và index | `OrderService.OpenInvoiceAsync`, `01_schema.sql`, `06_branch_management.sql` |
| Có thể chuyển bàn hoặc gộp hóa đơn khác chi nhánh | Chặn chuyển bàn đích khác chi nhánh, chặn bàn nguồn không thuộc chi nhánh hóa đơn và chặn gộp hai hóa đơn khác chi nhánh | `OrderService.MoveTableAsync`, `OrderService.MergeInvoicesAsync` |
| Dữ liệu các chi nhánh bị nhìn thấy lẫn nhau | Thêm query filter toàn cục theo `CurrentUser.BranchId` cho 13 entity/view: `Attendance`, `DiningTable`, `Employee`, `EmployeeShift`, `EmployeeSkill`, `Invoice`, `InvoiceDetail`, `InvoiceTable`, `Payment`, `Schedule`, `TableTransferLog`, `UniformIssue`, `VwPayroll` | `RestaurantDbContext.cs` |
| Hàng đợi bếp chưa tách chi nhánh | Lọc `vw_KitchenQueue` qua `EXISTS` trên `Invoice.BranchId` (view chưa có cột `BranchId`) | `OrderService.GetKitchenQueueAsync` |

## 5. Schema, dữ liệu mẫu và migration

- Thêm bảng `Branch` (mã duy nhất, tên, địa chỉ, điện thoại, trạng thái, audit, rowversion).
- Thêm `BranchId` cho `Employee`, `DiningTable`, `Invoice` (NOT NULL) và `AppUser` (NULL cho tài khoản hệ thống ADMIN).
- Thêm khóa ngoại `FK_*_Branch` và index `IX_Employee_BranchId`, `IX_DiningTable_BranchId`, `IX_Invoice_BranchId`.
- Seed hai chi nhánh `CN001` (Chi nhánh chính) và `CN002` (Chi nhánh 2); nhân viên, tài khoản và bàn mẫu thuộc `CN001`; thêm bàn `B11` thuộc `CN002`.
- `database/06_branch_management.sql` là migration bổ sung, idempotent cho database cũ: tạo bảng/chi nhánh mặc định, thêm cột, backfill (hóa đơn lấy chi nhánh từ bàn đầu tiên), chuyển cột sang NOT NULL, thay ràng buộc số bàn và tạo khóa ngoại/index.
- Đã sửa lỗi migration khi chạy trên database cũ chưa có cột `BranchId`: tách batch bằng `GO` sau các lệnh `ALTER TABLE` để SQL Server biên dịch phần backfill sau khi cột tồn tại; bật `ANSI_NULLS` và `QUOTED_IDENTIFIER` ở đầu script để cập nhật được bảng có filtered index/trigger.
- Thứ tự script cho database mới: `01` → `02` → `03` → `04` → `05` → `06`. Với database cũ: sao lưu, chạy `06_branch_management.sql`, sau đó chạy lại `03_views.sql`. Đã cập nhật `database/README.md` và `README.md`.

## 6. Kiểm thử và xác minh

- `dotnet build RestaurantManagement.slnx`: **thành công, 0 warning, 0 error**.
- `dotnet test RestaurantManagement.slnx --no-build`: **11 skipped, 0 failed** (bỏ qua vì thiếu chuỗi kết nối `RESTAURANT_TEST_CONNECTION_STRING`/SQL Server).
- Các ca kiểm thử liên quan chi nhánh đã được bổ sung: `Branch_scoped_queries_hide_other_branch_tables_and_invoices`, `MoveTable_rejects_a_destination_in_another_branch`, `MergeInvoices_rejects_invoices_from_different_branches`.
- Đã bổ sung `BranchId` cho dữ liệu `Invoice` và `Employee` được dựng thủ công trong `BusinessWorkflowTests.cs` để khớp schema mới.
- Kiểm tra tĩnh EF model: 36 entity, 13 query filter; các truy vấn đại diện (`VwPayroll`, hàng đợi bếp, `Employee`, `Attendance`) dịch được sang SQL.
- Ngày 13/11/2026 đã sao lưu database demo (`database/backup/RestaurantManagement_20261113_pre_branch.bak`), chạy `06_branch_management.sql` rồi chạy lại `03_views.sql` trên SQL Server Docker. Kết quả: 2 chi nhánh, 4 khóa ngoại chi nhánh, ràng buộc `UQ_DiningTable_Branch_TableNumber`, `vw_Payroll` có cột `BranchId`, không còn `BranchId` NULL trên `Employee`/`Invoice`.
- Chạy thử ứng dụng ở môi trường Development: khởi động thành công tại `http://localhost:5010`, trang chủ trả `HTTP 200`, không có lỗi runtime.

## 7. Hạn chế còn lại

- `AuditLog` chưa được lọc theo chi nhánh và chưa có trigger audit cho bảng `Branch`.
- `vw_KitchenQueue` chưa có cột `BranchId`; việc lọc hiện dùng truy vấn `EXISTS` trong `OrderService`.
- Danh mục, món, bảng giá, ca làm và khu vực (`Area`) vẫn dùng chung toàn hệ thống; chưa có thực đơn/giá riêng theo chi nhánh.
- Một số tài liệu lịch sử vẫn ghi 25 bảng, trong khi schema hiện có 26 bảng sau khi thêm `Branch`.
- Chưa chạy kiểm thử tích hợp và kiểm thử giao diện trong môi trường này.

## 8. Tệp thay đổi chính

**Cơ sở dữ liệu**

- `database/01_schema.sql`, `database/02_seed.sql`, `database/03_views.sql`, `database/06_branch_management.sql`
- `database/README.md`, `README.md`

**Ứng dụng**

- `Domain/Entities/Branch.cs` và `BranchId` trên `AppUser`, `Employee`, `DiningTable`, `Invoice`, các entity view.
- `Application/Abstractions/IBranchService.cs`, `ICurrentUserService.cs`, `IAccountService.cs`
- `Application/Models/Branches/`, `Accounts/`, `Employees/`, `Tables/`, `AuthenticatedUser.cs`, `UserSessionState.cs`
- `Infrastructure/Branches/BranchService.cs`, `Accounts/AccountService.cs`, `Employees/EmployeeService.cs`, `Orders/OrderService.cs`, `Reporting/ReportingService.cs`, `Tables/TableManagementService.cs`, `Persistence/RestaurantDbContext.cs`, `Authentication/UserAuthenticationService.cs`
- `Web/Controllers/BranchesController.cs`, `AccountsController.cs`, `EmployeesController.cs`, `TablesController.cs`, `AccountController.cs`, `ReportsController.cs`
- `Web/Views/Branches/`, `Accounts/`, `Employees/`, `Tables/`, `Shared/_Layout.cshtml`
- `Tests/BusinessWorkflowTests.cs`
