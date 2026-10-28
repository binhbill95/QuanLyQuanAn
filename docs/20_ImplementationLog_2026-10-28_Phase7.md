# Implementation Log — Phase 7: Thanh toán (2026-10-28)

## Phạm vi

Triển khai tính tiền, ghi nhận một lần thanh toán cho mỗi hóa đơn, đóng hóa đơn, giải phóng các bàn liên kết và hiển thị hóa đơn có thể in. Dùng schema `Invoice`/`Payment` hiện có, không đổi CSDL.

**Trạng thái:** Hoàn tất triển khai, build và kiểm thử end-to-end.

## Kết quả

- Trang `/Payments` liệt kê các hóa đơn đang phục vụ; `/Payments/Checkout` hiển thị món, bàn, khách và tạm tính.
- Hỗ trợ tiền mặt và chuyển khoản; ghi đúng số tiền sau giảm giá vào `Payment.Amount`.
- Chỉ MANAGER được nhập giảm giá; CASHIER được thanh toán nhưng không thể tự áp giảm giá. ADMIN không được thanh toán vì schema yêu cầu `PaidByEmployeeId`, trong khi tài khoản ADMIN không gắn hồ sơ nhân viên.
- Nếu còn món chờ hoặc đang chế biến, giao diện cảnh báo và yêu cầu xác nhận; service kiểm tra lại xác nhận trong transaction trước khi ghi giao dịch.
- Ghi `Payment`, `Invoice.DiscountAmount`, trạng thái đã thanh toán và `ClosedAt`; giải phóng mọi liên kết bàn trong cùng transaction `Serializable`.
- Trang `/Payments/Receipt` hiển thị thông tin hóa đơn, dòng món không bị hủy, giảm giá, phương thức/người thu tiền và nút in qua trình duyệt.

## Kiểm chứng

| Kiểm tra | Kết quả |
|----------|---------|
| `dotnet build src\RestaurantManagement.slnx --no-restore` | Thành công, 0 warning / 0 error |
| Đăng nhập MANAGER, `GET /Payments`, `GET /Orders` | HTTP 200 |
| Thanh toán khi còn món chờ, không gửi xác nhận | Bị từ chối; hóa đơn, thanh toán và liên kết bàn không thay đổi |
| Xác nhận thanh toán hóa đơn còn món chờ | Thành công |
| Giảm giá MANAGER 5.000 trên tạm tính 90.000, phương thức chuyển khoản | `Payment.Amount` = 85.000; lưu giảm giá và phương thức chính xác |
| Đóng hóa đơn và giải phóng bàn | `Invoice.Status = 1`, `ClosedAt` có giá trị, liên kết bàn đã giải phóng |
| Trang hóa đơn thanh toán | Hiển thị đúng mã hóa đơn, phương thức và nội dung biên nhận; có thao tác in |
| Dọn dữ liệu E2E và kiểm tra baseline | Thành công; hóa đơn mở, liên kết bàn hoạt động, chi tiết món, thanh toán và log chuyển bàn đều trở về 0 |

## Ghi chú triển khai

- Transaction dùng `CreateExecutionStrategy()` và mức cô lập `Serializable`, phù hợp cấu hình retry SQL Server và ngăn thanh toán trùng đồng thời.
- Discount được kiểm tra quyền trong service, ngoài policy/controller; mức giảm không âm, tối đa 2 chữ số thập phân và không vượt tạm tính.
- Hóa đơn không có món hợp lệ hoặc không còn bàn phục vụ không thể thanh toán.
- Một hóa đơn chỉ có một giao dịch thanh toán theo unique index hiện có; không hỗ trợ tách phương thức/thanh toán nhiều lần.
- Tiếp theo: Phase 8 — báo cáo.
