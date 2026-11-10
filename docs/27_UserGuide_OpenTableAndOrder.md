# Hướng dẫn mở bàn và gọi món

Hướng dẫn này mô tả quy trình khi khách đến quán: chọn bàn trống, mở lượt phục vụ/tạo hóa đơn, rồi gọi món vào hóa đơn đó.

## Vai trò và điều kiện

- Đăng nhập bằng vai trò có quyền gọi món: `ADMIN`, `OWNER`, `MANAGER`, `WAITER` hoặc `CASHIER`.
- Tài khoản dùng để mở bàn phải được gắn với hồ sơ nhân viên đang hoạt động. Nếu chưa gắn, hệ thống không thể ghi nhận nhân viên tiếp nhận.
- Bàn cần đang hoạt động và chưa có lượt phục vụ khác.

## Quy trình thao tác

1. Đăng nhập ứng dụng, mở **Gọi món** (Bán hàng/POS).
2. Xem các thẻ bàn. Mỗi thẻ hiển thị số bàn, khu vực, sức chứa và trạng thái:
   - **Trống**: có thể chọn để mở lượt phục vụ mới.
   - **Có khách**, **Đang đợi món**, **Món đã xong**: bàn đang thuộc một lượt phục vụ.
3. Chọn thẻ của bàn **Trống**. Biểu mẫu **Mở bàn [số bàn]** hiển thị sức chứa tối đa.
4. Nhập **Số lượng khách** thực tế, không nhỏ hơn 1 và không vượt sức chứa bàn.
5. Nhấn **Mở bàn và tạo hóa đơn**. Ứng dụng tạo hóa đơn đang phục vụ, ghi nhận nhân viên đăng nhập và chuyển đến màn hình hóa đơn mới.
6. Trong mục **Gọi thêm món**, chọn món còn bán, nhập số lượng, thêm ghi chú nếu khách có yêu cầu, rồi nhấn **Thêm món**. Có thể lặp lại thao tác này mỗi khi khách gọi thêm.
7. Kiểm tra món đã xuất hiện trong danh sách hóa đơn với trạng thái **Đang chờ**. Món được đưa vào hàng đợi bếp theo thứ tự gọi.

## Kết quả mong đợi

- Trước khi gọi món, hóa đơn mới hiển thị **Chưa gọi món**; đây là trạng thái hợp lệ sau khi mở bàn.
- Sau khi thêm món, hóa đơn hiển thị tên món, số lượng, đơn giá, thành tiền và trạng thái.
- Bàn chuyển từ **Trống** sang trạng thái đang phục vụ. Sau khi bếp cập nhật trạng thái món, trạng thái bàn được tính lại theo món trong hóa đơn.
- Giá bán được chụp vào chi tiết hóa đơn tại thời điểm gọi món; thay đổi giá thực đơn sau đó không làm đổi giá món đã gọi.

## Nếu thao tác không thành công

| Thông báo/tình huống | Cách xử lý |
|---|---|
| Không thể mở bàn vì tài khoản chưa gắn hồ sơ nhân viên | Quản trị viên kiểm tra tài khoản đã gắn đúng hồ sơ nhân viên đang hoạt động chưa. |
| Bàn không tồn tại hoặc đã ngừng phục vụ | Chọn bàn đang hoạt động khác hoặc nhờ quản lý kiểm tra cấu hình bàn. |
| Bàn đang có hóa đơn phục vụ | Không mở hóa đơn mới; chọn bàn đó để mở lượt phục vụ/hóa đơn hiện tại. |
| Số khách vượt sức chứa | Điều chỉnh số khách hoặc chọn bàn đủ chỗ. Không nên nhập số khách thấp hơn thực tế để vượt kiểm tra. |
| Món không còn bán hoặc nhóm món đã ngừng | Báo khách chọn món khác; nhờ quản lý kiểm tra tình trạng món nếu cần. |
| Hóa đơn mở nhưng danh sách món ghi “Chưa gọi món” | Đây là bình thường. Chọn món trong **Gọi thêm món** và nhấn **Thêm món**. |

## Lưu ý trong quá trình phục vụ

- Không tạo lượt phục vụ mới cho bàn đã có khách; mở hóa đơn hiện tại để gọi thêm món.
- Chỉ món còn **Đang chờ** mới có thể hủy hoặc đổi trên hệ thống; cần nhập lý do. Món bếp đã nhận chế biến không thể hủy theo luồng thông thường.
- Không thanh toán hóa đơn nếu khách chưa yêu cầu. Khi thanh toán, hệ thống sẽ đóng hóa đơn và giải phóng các bàn đang gắn với hóa đơn.
- Khi kiểm thử UI trên database dùng chung, chỉ tạo dữ liệu demo có thể dọn an toàn. Không xóa Docker volume vì thao tác đó sẽ xóa toàn bộ dữ liệu database.

## Luồng tóm tắt

```text
Khách đến
  → Nhân viên mở Gọi món
  → Chọn bàn Trống
  → Nhập số khách
  → Mở bàn và tạo hóa đơn
  → Chọn món, số lượng, ghi chú
  → Thêm món vào hóa đơn
  → Món xuất hiện trong hàng đợi bếp
```
