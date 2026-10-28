# Implementation Log — Phase 5: Thực đơn, nhóm món và bàn ăn (2026-10-28)

## Phạm vi

Hoàn thiện Phase 5 trên nền schema đã có trong SQL Server: nhóm món (`Category`), món ăn/nước uống (`MenuItem`), khu vực (`Area`) và bàn (`DiningTable`). Không thay đổi schema hoặc migration.

## Kết quả

- Trang thực đơn cho người dùng đã đăng nhập: tìm kiếm, lọc nhóm, xem giá/đơn vị/mô tả/ảnh và trạng thái còn món.
- ADMIN/OWNER/MANAGER quản lý nhóm món, món và trạng thái sử dụng; Bếp có thể đánh dấu món còn/hết.
- Người dùng phục vụ và thu ngân chỉ thấy món còn bán trong nhóm đang hoạt động.
- Quản lý khu vực và bàn: thêm/sửa/xóa mềm, sức chứa và trạng thái bàn.
- Sơ đồ bàn dùng `vw_DiningTableStatus` để hiển thị Trống, Có khách, Đang đợi món, Món đã xong.
- Ngăn thay đổi/xóa/ngừng bàn đang gắn với hóa đơn chưa giải phóng; ngăn xóa khu vực còn bàn.
- Thêm liên kết thực đơn, nhóm món, bàn ăn vào điều hướng và trang chủ.

## Kiểm chứng

| Kiểm tra | Kết quả |
|----------|---------|
| `dotnet build src\RestaurantManagement.slnx --no-restore` | Thành công, 0 warning / 0 error |
| `GET /health/db` | healthy; SQL Server có 4 nhân viên seed |
| Đăng nhập ADMIN và `GET /Menu` | 200; hiển thị 14 món seed |
| Đăng nhập ADMIN và `GET /Categories` | 200 |
| Đăng nhập ADMIN và `GET /Tables` | 200; hiển thị 10 bàn seed |
| Đăng nhập ADMIN và `GET /Tables/Areas` | 200; hiển thị 3 khu vực seed |
| Dữ liệu | Chỉ đọc; không tạo dữ liệu test |

## Tệp triển khai

- Application: DTO và `ITableManagementService`.
- Infrastructure: bổ sung quy tắc lọc nhóm món hoạt động; triển khai nghiệp vụ quản lý khu vực/bàn và kiểm tra bàn đang phục vụ.
- Web: hoàn thiện màn thực đơn, thêm màn sơ đồ bàn và quản lý khu vực; cập nhật điều hướng và trang chủ.
- Tài liệu: cập nhật roadmap và `src/README.md`.

## Tiếp theo

Phase 6: xử lý hóa đơn/gọi món, thêm món nhiều lần, trạng thái món tại bếp và nghiệp vụ chuyển/gộp bàn.
