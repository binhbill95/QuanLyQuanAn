# Implementation Log — Nhật ký thao tác (2026-10-28)

## Phạm vi

Hoàn thiện audit trail hiện có: gắn tài khoản thực hiện vào trigger log, bổ sung coverage cho các nghiệp vụ quản trị và tạo trang tra cứu phân quyền. Không thêm bảng hoặc thay đổi schema.

## Kết quả

- Mở rộng `database/04_audit.sql` thành 10 trigger cho `AppUser`, `Employee`, `Category`, `MenuItem`, `Shift`, `Schedule`, `Attendance`, `Invoice`, `InvoiceDetail`, `Payment`.
- `RestaurantDbContext` đặt `ChangedByUserId` qua `sys.sp_set_session_context` trong đúng SQL connection của mỗi lần `SaveChanges`; context được xóa sau thao tác để tránh giữ actor trong connection pool.
- Khai báo trigger với EF Core trên các bảng tương ứng để SQL Server không gặp lỗi `OUTPUT` khi ghi.
- Trigger tài khoản bỏ qua thay đổi `LastLoginAt`; không ghi `PasswordHash`. Đổi mật khẩu chỉ tạo cờ `PasswordChanged` trong JSON đã lọc.
- Trang `/Audit` (ADMIN/OWNER/MANAGER) gộp nhật ký dữ liệu và `TableTransferLog`, hỗ trợ lọc theo khoảng ngày, thao tác, từ khóa; có phân trang và xem JSON trước/sau.
- Ngày nhập trên bộ lọc được hiểu theo giờ Việt Nam và chuyển về UTC để so khớp với dữ liệu lưu.

## Kiểm chứng

| Kiểm tra | Kết quả |
|----------|---------|
| Áp dụng `database/04_audit.sql` trên SQL Server trong Docker | Thành công; 10 trigger audit được kích hoạt |
| Build `dotnet build src\RestaurantManagement.slnx --no-restore` | Thành công, 0 warning / 0 error |
| Mở `/Audit` với MANAGER | HTTP 200; hiển thị nhật ký cũ và phân biệt các bản ghi lịch sử chưa có actor |
| Lọc `Operation=UPDATE` | Chỉ trả các hoạt động cập nhật |
| Lưu nhóm món qua ứng dụng | Thành công; trigger ghi `Category` và `ChangedByUserId` trỏ tới tài khoản `manager` |
| Kiểm tra payload của bản ghi mới | Không chứa `PasswordHash` |
| Kiểm tra trigger `AppUser` trong transaction rollback | Bỏ qua cập nhật chỉ đổi `LastLoginAt`; đổi mật khẩu tạo cờ `PasswordChanged`, không lộ hash; transaction được rollback |
| Làm mới bộ backup cho giảng viên | `.bak` qua `RESTORE VERIFYONLY`; full SQL restore thử thành công: 25 bảng, 10 view, 10 trigger audit, 98 dòng |

## Ghi chú

- Các bản ghi trước khi triển khai vẫn giữ nguyên `ChangedByUserId = NULL`; không thể khôi phục danh tính người thao tác trong quá khứ.
- Lịch sử chuyển/gộp bàn lấy người thực hiện từ hồ sơ nhân viên trong `TableTransferLog`.
- Audit log chứa dữ liệu nghiệp vụ nhạy cảm từ các hàng được theo dõi; trang chỉ dành cho ADMIN/OWNER/MANAGER.
- SQL Server đang giữ 7 audit rows lịch sử và thêm row kiểm chứng Category do thao tác qua giao diện; không xóa lịch sử này.
