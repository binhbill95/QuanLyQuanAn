# Implementation Log — Khôi phục món đã xóa mềm (2026-10-28)

## Phạm vi

Thêm chức năng để ADMIN, OWNER và MANAGER tra cứu, khôi phục món bị xóa mềm từ trang thực đơn. Không xóa vật lý dữ liệu hay hóa đơn lịch sử.

## Kết quả

- Trang `/Menu` có chế độ xem riêng “Món đã xóa”; danh sách thông thường tiếp tục chỉ hiển thị món đang hoạt động.
- Mỗi món đã xóa có thể được khôi phục vào một nhóm đang hoạt động. Chọn lại nhóm cho phép khôi phục an toàn ngay cả khi nhóm ban đầu đã bị ngừng hoặc xóa mềm.
- Khôi phục đặt lại `IsDeleted` và `DeletedAt`, đồng thời cập nhật `CategoryId`, `UpdatedAt` và `UpdatedBy`. Mã, tên, giá, mô tả, ảnh và tình trạng còn/hết được giữ nguyên.
- Không có nhóm đang hoạt động thì thao tác khôi phục bị vô hiệu hóa; service cũng xác thực lại nhóm ở phía máy chủ.
- Khôi phục dùng luồng EF Core hiện có, do đó trigger audit MenuItem ghi lại thay đổi như các thao tác chỉnh sửa khác.

## Kiểm chứng

| Kiểm tra | Kết quả |
|----------|---------|
| Build solution với output tạm riêng và output ứng dụng | Thành công, 0 warning / 0 error |
| Mở `/Menu?showDeleted=true` bằng phiên MANAGER | Trang tải thành công, hiển thị đúng trạng thái rỗng (CSDL hiện không có món đã xóa) và nút quay về thực đơn |
| Kiểm tra quyền và chống giả mạo POST | Xem món đã xóa chỉ bật cho ADMIN/OWNER/MANAGER; endpoint khôi phục yêu cầu policy Management và antiforgery |
| Kiểm tra tính hợp lệ của nhóm đích | Service chỉ nhận nhóm còn tồn tại, đang hoạt động; nút khôi phục bị vô hiệu hóa nếu chưa có nhóm phù hợp |
| Kiểm tra bảo toàn dữ liệu lịch sử | Khôi phục xóa mềm; không xóa vật lý và không thay đổi `InvoiceDetail.UnitPrice` |

## Ghi chú kiểm thử

Chưa chạy thao tác khôi phục đầu-cuối vì dữ liệu hiện tại không có món xóa mềm; không tạo hoặc sửa dữ liệu nhà hàng chỉ để thử nghiệm. Có thể kiểm thử thao tác này sau khi có món trong danh sách “Món đã xóa”.
