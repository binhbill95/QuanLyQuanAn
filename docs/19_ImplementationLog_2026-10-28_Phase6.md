# Implementation Log — Phase 6: Gọi món và xử lý tại bếp (2026-10-28)

## Phạm vi

Triển khai luồng từ mở bàn đến bếp xử lý món, dựa trên schema hiện có (`Invoice`, `InvoiceTable`, `InvoiceDetail`, `TableTransferLog`) và hai view `vw_DiningTableStatus`, `vw_KitchenQueue`. Không đổi schema.

**Trạng thái:** Hoàn tất triển khai, build và kiểm thử end-to-end các thao tác ghi hợp lệ.

## Kết quả

- Sơ đồ bàn phục vụ gọi món, xem trạng thái và hóa đơn đang mở.
- Mở hóa đơn cho bàn trống, ghi số khách/nhân viên, chặn quá sức chứa; thao tác mở bàn chạy trong transaction `Serializable`.
- Thêm món nhiều lần, ghi chú, chụp giá hiện hành vào `InvoiceDetail.UnitPrice`.
- Hủy món khi còn chờ, yêu cầu lý do; đổi món nguyên tử (hủy món chờ + thêm món thay thế).
- Màn hình bếp hiển thị FIFO, thời gian chờ, bàn/hóa đơn và ghi chú; trạng thái chỉ chuyển `Chờ → Đang chế biến → Hoàn thành`.
- Chuyển hóa đơn sang bàn trống và gộp hóa đơn; ghi `TableTransferLog`, kiểm tra trạng thái hóa đơn và sức chứa gộp.
- Phân quyền dùng policy `OrderManagement` cho ADMIN/OWNER/MANAGER/WAITER/CASHIER và `Kitchen` cho ADMIN/OWNER/MANAGER/KITCHEN.

## Kiểm chứng

| Kiểm tra | Kết quả |
|----------|---------|
| `dotnet build src\RestaurantManagement.slnx --no-restore` | Thành công, 0 warning / 0 error |
| Đăng nhập MANAGER, `GET /Orders` | HTTP 200 |
| Đăng nhập MANAGER, `GET /Kitchen/Queue` | HTTP 200 |
| Mở bàn với số khách vượt sức chứa / mở lại bàn đang phục vụ | Cả hai trường hợp bị từ chối đúng quy tắc |
| Mở 2 bàn, gọi món và kiểm tra hàng đợi bếp | Thành công; hóa đơn được tạo và món xếp FIFO theo thời điểm gọi |
| Hủy món đang chờ / đổi món đang chờ | Thành công; món cũ được đánh dấu hủy, món thay thế chụp đúng giá hiện hành |
| Bếp nhận chế biến / hoàn thành món | Thành công; không cho hủy món sau khi bếp đã nhận |
| Chuyển bàn / gộp hóa đơn | Thành công; giữ lịch sử bàn, đóng hóa đơn nguồn và cộng đúng tổng tiền |
| Đối chiếu trạng thái và số tiền trực tiếp trong CSDL | Hóa đơn đích còn mở với 4 khách, subtotal 110.000; hóa đơn nguồn đã đóng |
| Dọn dữ liệu E2E và kiểm tra baseline | Thành công; hóa đơn mở, liên kết bàn hoạt động, chi tiết món và log chuyển bàn đều trở về 0 |

## Ghi chú triển khai

- Transaction tường minh chạy bên trong `CreateExecutionStrategy()` vì SQL Server được cấu hình retry.
- Trigger CSDL vẫn là lớp bảo vệ cuối cho hủy món sai trạng thái và sửa hóa đơn không còn mở.
- Harness E2E lấy mã hóa đơn qua liên kết bàn đang hoạt động trong CSDL, tránh phụ thuộc vào URL redirect của PowerShell; các tổng tiền được đối chiếu trong CSDL để không phụ thuộc định dạng phân cách hàng nghìn theo locale.
- Toàn bộ bản ghi thử, gồm hóa đơn, chi tiết, liên kết bàn, log chuyển bàn và audit liên quan, đã được dọn sau kiểm thử.
- Phase 7 tiếp tục luồng đóng hóa đơn, thanh toán và in hóa đơn.
