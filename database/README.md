# Database — Restaurant Management System

Thư mục chứa toàn bộ script CSDL cho **SQL Server 2022** (chạy trong Docker).

## Thứ tự chạy

| Thứ tự | Tệp | Nội dung |
|--------|-----|----------|
| 1 | `01_schema.sql` | Tạo database `RestaurantManagement`, bảng, PK/FK, CHECK, UNIQUE, INDEX |
| 2 | `02_seed.sql` | Dữ liệu mẫu: 6 vai trò, 2 chi nhánh, 4 vị trí, 4 kỹ năng, 2 ca, 11 bàn, 14 món, tài khoản mẫu |
| 3 | `03_views.sql` | 10 view phục vụ trạng thái bàn, hàng đợi bếp, doanh thu, khách theo ca/nhân viên/bàn, bảng lương |
| 4 | `04_audit.sql` | 10 trigger ghi `AuditLog` (tài khoản, nhân sự, danh mục/món, ca/lịch/chấm công, hóa đơn/thanh toán) |
| 5 | `05_rules.sql` | 2 trigger ràng buộc nghiệp vụ: chỉ hủy món đang chờ; không thêm món vào hóa đơn đã thanh toán |
| 6 | `06_branch_management.sql` | Bổ sung chi nhánh cho database đã có dữ liệu; idempotent, backfill sang `CN001` |

## Kiểm chứng đã chạy

- 26 bảng, 10 view, 10 trigger audit + 2 trigger quy tắc nghiệp vụ.
- Luồng demo: mở bàn → gọi món → bếp xong → thanh toán → báo cáo → dọn dẹp.
- Xem kết quả chi tiết tại `../docs/12_ImplementationLog.md`.

## Chạy tự động qua Docker

Xem `../docker/README.md`. Container tự khởi tạo schema, seed, view và trigger khi database chưa tồn tại.

## Kiểm thử nghiệp vụ tích hợp

Khi container SQL Server đang chạy, chạy `.\database\run_automation_tests.ps1` từ thư mục gốc repo. Script tạo/cập nhật `RestaurantManagement_AutomationTest` riêng, áp dụng schema/seed/views/triggers rồi chạy test; không kết nối hoặc sửa database demo `RestaurantManagement`.

## Chạy thủ công

```bash
sqlcmd -S localhost,1433 -U sa -P "<MSSQL_SA_PASSWORD>" -C -b -I -i 01_schema.sql
sqlcmd -S localhost,1433 -U sa -P "<MSSQL_SA_PASSWORD>" -C -b -I -i 02_seed.sql
sqlcmd -S localhost,1433 -U sa -P "<MSSQL_SA_PASSWORD>" -C -b -I -i 03_views.sql
sqlcmd -S localhost,1433 -U sa -P "<MSSQL_SA_PASSWORD>" -C -b -I -i 04_audit.sql
sqlcmd -S localhost,1433 -U sa -P "<MSSQL_SA_PASSWORD>" -C -b -I -i 05_rules.sql
sqlcmd -S localhost,1433 -U sa -P "<MSSQL_SA_PASSWORD>" -C -b -I -i 06_branch_management.sql
```

Với database cũ chưa có chi nhánh, sao lưu trước rồi chạy `06_branch_management.sql`, sau đó chạy lại `03_views.sql` (view có thêm cột `BranchId`). Script tạo `CN001`/`CN002`, gán dữ liệu hiện có về `CN001`, rồi bổ sung khóa ngoại. Chi nhánh được lưu trên tài khoản, bàn và hóa đơn; thực đơn/giá vẫn dùng chung.

## Tài khoản mẫu

| Username | Mật khẩu | Vai trò | Ghi chú |
|----------|----------|---------|---------|
| `admin` | `Admin@123` | ADMIN | Không gắn nhân viên |
| `manager` | `Admin@123` | MANAGER | Gắn nhân viên NV001 |

> Hash mật khẩu dùng định dạng ASP.NET Identity V3 (PBKDF2-HMAC-SHA512, 210.000 vòng). **Đổi mật khẩu ngay khi dùng thật.**

## Quy ước

- Tên bảng/cột: PascalCase số ít (`Invoice`, `InvoiceDetail`).
- Khóa chính: `Id` (int identity); khóa ngoại: `<Entity>Id`.
- Audit: `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt`, `RowVersion`.
- Thời gian lưu **UTC** (`SYSUTCDATETIME()`), hiển thị đổi sang `Asia/Ho_Chi_Minh` ở tầng ứng dụng.
- Không xóa cứng dữ liệu đã phát sinh; dùng `IsDeleted`/`Status`.
- Không cascade xóa — toàn bộ FK dùng `NO ACTION` (trừ 2 FK `ON DELETE SET NULL` ở `Position.DefaultRoleId`, `DiningTable.AreaId`, `AppUser.EmployeeId`).

## Đối chiếu tài liệu

- Thiết kế chi tiết: `../docs/11_DatabaseSchemaDesign.md`
- Yêu cầu CSDL ban đầu: `../docs/06_DatabaseRequirements.md`
- Nhật ký thực hiện: `../docs/12_ImplementationLog.md`
