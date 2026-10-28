# Use Cases

> Suy ra từ `10_Quan-ly-quan-an.pdf`: 2 quy trình nghiệp vụ + danh sách chức năng theo từng actor + ghi chú nghiệp vụ (gọi nhiều lần, hủy món, chuyển bàn, ghép bàn).
> Mức ưu tiên: **B** = bắt buộc, **T** = tùy chọn.

## 1. Danh sách Use Case

| Mã | Tên Use Case | Actor | Nhóm | Ưu tiên |
|----|--------------|-------|------|---------|
| UC001 | Đăng nhập | Tất cả | Xác thực | B |
| UC002 | Đổi mật khẩu | Phục vụ, Quản lý, Chủ quán, Bếp | Xác thực | B |
| UC003 | Đăng xuất | Tất cả | Xác thực | B |
| UC004 | Xem sơ đồ bàn và trạng thái bàn | Phục vụ, Quản lý, Chủ quán | Bàn | B |
| UC005 | Mở bàn / tạo hóa đơn cho bàn | Phục vụ, Quản lý | Bàn & đơn | B |
| UC006 | Gọi món (thêm món vào hóa đơn) | Phục vụ, Quản lý | Đơn | B |
| UC007 | Đổi món | Phục vụ, Quản lý | Đơn | B |
| UC008 | Hủy món | Phục vụ, Quản lý | Đơn | B |
| UC009 | Chuyển bàn | Phục vụ, Quản lý | Bàn & đơn | B |
| UC010 | Ghép bàn | Phục vụ, Quản lý | Bàn & đơn | B |
| UC011 | Tính tiền và in hóa đơn | Phục vụ, Quản lý | Thanh toán | B |
| UC012 | Xem hàng đợi món tại bếp | Bếp | Bếp | B |
| UC013 | Cập nhật trạng thái món hoàn thành | Bếp | Bếp | B |
| UC014 | Quản lý nhân viên (thêm/sửa/xóa, xem hồ sơ) | Chủ quán | Nhân sự | B |
| UC015 | Cấu hình danh mục trình độ vi tính | Chủ quán | Nhân sự | B |
| UC016 | Cấu hình ca làm việc | Chủ quán | Nhân sự | B |
| UC017 | Xếp lịch làm việc theo tháng | Chủ quán | Nhân sự | B |
| UC018 | Xem lịch làm việc của bản thân | Phục vụ, Bếp | Nhân sự | B |
| UC019 | Xem lịch làm việc chung / của một nhân viên | Quản lý, Chủ quán | Nhân sự | B |
| UC020 | Chấm công (bắt đầu / kết thúc làm) | Phục vụ, Bếp | Nhân sự | B |
| UC021 | Duyệt chấm công | Quản lý, Chủ quán | Nhân sự | B |
| UC022 | Tính lương theo số ca trong tháng | Chủ quán | Nhân sự | B |
| UC023 | Cho nhân viên nghỉ việc (đổi trạng thái, xóa lịch, trả dụng cụ) | Chủ quán | Nhân sự | B |
| UC024 | Tổ chức bàn ghế (số thứ tự, số người tối đa) | Chủ quán | Bàn | B |
| UC025 | Quản lý thực đơn (món, loại món, giá) | Chủ quán | Thực đơn | B |
| UC026 | Đánh dấu món hết / còn | Chủ quán, Bếp | Thực đơn | B |
| UC027 | Báo cáo doanh thu tháng/quý/năm | Chủ quán | Báo cáo | B |
| UC028 | Báo cáo số lượng khách theo ca | Chủ quán | Báo cáo | B |
| UC029 | Báo cáo số khách một nhân viên phục vụ trong tuần | Chủ quán, Quản lý | Báo cáo | B |
| UC030 | Báo cáo số lượng khách của bàn | Chủ quán | Báo cáo | B |
| UC031 | Báo cáo số lượng bán của món | Chủ quán | Báo cáo | B |
| UC032 | Báo cáo số ca làm việc của nhân viên | Chủ quán, Quản lý | Báo cáo | B |
| UC033 | Phân quyền người dùng (loại người dùng, chức năng) | Chủ quán | Hệ thống | T |
| UC034 | Backup / Restore dữ liệu | Chủ quán | Hệ thống | T |

## 2. Chi tiết các Use Case trọng tâm

### UC001 — Đăng nhập
- **Actor:** tất cả người dùng nội bộ.
- **Tiền điều kiện:** tài khoản đã được tạo và đang ở trạng thái hoạt động.
- **Luồng chính:** nhập tên đăng nhập + mật khẩu → hệ thống xác thực → mở màn hình mặc định theo vai trò (Phục vụ: sơ đồ bàn; Bếp: hàng đợi món; Quản lý/Chủ quán: tổng quan).
- **Luồng thay thế:** sai mật khẩu → thông báo lỗi, cho phép nhập lại; tài khoản bị khóa (nhân viên đã nghỉ) → từ chối đăng nhập.
- **Hậu điều kiện:** phiên đăng nhập được tạo, các chức năng bị giới hạn theo quyền.

### UC004 — Xem sơ đồ bàn và trạng thái bàn
- **Luồng chính:** hiển thị danh sách bàn → mỗi bàn kèm trạng thái **trống / có khách / đang đợi món / món đã xong** → chọn bàn để xem hóa đơn hiện tại.
- **Quy tắc:** trạng thái "đang đợi món"/"món đã xong" phải phản ánh trạng thái món trong hóa đơn, không cho nhập tay tùy ý.

### UC005 — Mở bàn / tạo hóa đơn cho bàn
- **Tiền điều kiện:** bàn đang trống.
- **Luồng chính:** chọn bàn → nhập số lượng khách → ghi nhận nhân viên tiếp nhận (mặc định là người đang đăng nhập) → tạo hóa đơn "đang phục vụ" → chuyển trạng thái bàn sang "có khách".
- **Luồng thay thế:** bàn đã có hóa đơn chưa thanh toán → không tạo mới, mở hóa đơn hiện tại (tránh trùng hóa đơn).

### UC006 — Gọi món (thêm món)
- **Tiền điều kiện:** bàn/hóa đơn đang phục vụ.
- **Luồng chính:** chọn món từ thực đơn → nhập số lượng và ghi chú → kiểm tra món còn → thêm dòng món vào hóa đơn với trạng thái "đang chờ" → món xuất hiện trong hàng đợi bếp theo thời gian đặt.
- **Luồng thay thế:** món đã hết → chặn thêm, thông báo để nhân viên báo lại khách.
- **Quy tắc:** được gọi nhiều lần cho cùng một bàn (BR-01).

### UC007 — Đổi món
- **Luồng chính:** chọn dòng món cần đổi → kiểm tra trạng thái "đang chờ" → hủy dòng cũ (ghi lý do "đổi món") → thêm món mới cùng số lượng → thông báo bếp nếu món mới.
- **Luồng thay thế:** món đã bắt đầu chế biến → không cho đổi, chỉ hướng dẫn xử lý thủ công.

### UC008 — Hủy món
- **Tiền điều kiện:** món ở trạng thái "đang chờ" (chưa thực hiện).
- **Luồng chính:** chọn dòng món → nhập lý do hủy → đánh dấu "đã hủy" → loại khỏi hàng đợi bếp và khỏi tiền hóa đơn.
- **Luồng thay thế:** món đã hoàn thành/đang chế biến → từ chối hủy (BR-02).

### UC009 — Chuyển bàn
- **Luồng chính:** chọn hóa đơn hiện tại → chọn bàn đích đang trống → chuyển toàn bộ hóa đơn sang bàn mới → bàn cũ về "trống", bàn mới "có khách" → ghi log chuyển bàn (thời gian, người thực hiện, bàn đi, bàn đến).
- **Luồng thay thế:** bàn đích đang có khách → yêu cầu dùng chức năng ghép bàn.

### UC010 — Ghép bàn
- **Luồng chính:** chọn 2+ bàn cần tính chung → hệ thống gộp các dòng món của các hóa đơn → tạo/xác nhận một hóa đơn tổng → khi thanh toán, các bàn liên quan cùng được giải phóng.
- **Quy tắc:** lưu liên kết hóa đơn ↔ bàn để báo cáo "khách của bàn" vẫn tách được theo từng bàn (BR-04, BR-07).

### UC011 — Tính tiền và in hóa đơn
- **Luồng chính:** chọn bàn/hóa đơn → hệ thống cộng tiền các món không bị hủy → áp giảm giá (nếu có) → xác nhận thanh toán (tiền mặt/chuyển khoản) → in hóa đơn → hóa đơn chuyển "đã thanh toán", bàn về "trống".
- **Luồng thay thế:** còn món chưa hoàn thành → cảnh báo, yêu cầu xác nhận trước khi thanh toán.
- **Hậu điều kiện:** doanh thu được ghi nhận cho báo cáo; số khách được ghi nhận cho thống kê theo ca/nhân viên/bàn.

### UC012 — Xem hàng đợi món tại bếp
- **Luồng chính:** hiển thị danh sách dòng món của các hóa đơn đang mở, sắp xếp tăng dần theo thời gian đặt → hiển thị bàn, tên món, số lượng, ghi chú, thời gian chờ.
- **Quy tắc:** món đã hủy không hiển thị; món đã hoàn thành chuyển khỏi hàng đợi (BR-06).

### UC013 — Cập nhật trạng thái món hoàn thành
- **Luồng chính:** chọn dòng món → đánh dấu "hoàn thành" → hệ thống ghi thời điểm → cập nhật trạng thái bàn thành "món đã xong" nếu tất cả món đã xong.
- **Luồng thay thế:** thao tác nhầm → cho phép hoàn tác về "đang chờ" trong thời gian ngắn (tùy chọn).

### UC014 — Quản lý nhân viên
- **Luồng chính:** thêm nhân viên với đầy đủ hồ sơ (họ tên, giới tính, ngày sinh, vị trí, điện thoại, địa chỉ, CMND, thời gian bắt đầu làm, trình độ vi tính, ca có thể làm, dụng cụ/trang phục) → lưu → cấp tài khoản đăng nhập tương ứng.
- **Luồng thay thế:** trùng CMND/số điện thoại → cảnh báo; thiếu trường bắt buộc → không lưu.

### UC016 — Cấu hình ca làm việc
- **Luồng chính:** xem danh sách ca → thêm/sửa/xóa ca (tên ca, giờ bắt đầu, giờ kết thúc) → lưu.
- **Quy tắc:** không xóa ca đã phát sinh lịch/chấm công (chỉ chuyển sang "ngừng dùng"); kiểm tra chồng lấn khung giờ (BR-10, BR-12).

### UC017 — Xếp lịch làm việc theo tháng
- **Luồng chính:** chọn tháng → hiển thị lưới 7 ngày/tuần × các ca/ngày → chọn ca → thêm nhân viên (chỉ những người có ca đó trong "ca có thể làm") → lưu → xem số nhân viên/ca, ca còn trống.
- **Luồng thay thế:** xếp trùng ca cùng ngày → chặn (BR-12).

### UC020 — Chấm công *(cơ bản)*
- **Luồng chính:** nhân viên mở màn hình chấm công → bấm "Bắt đầu làm" (lưu giờ vào) → cuối ca bấm "Kết thúc làm" (lưu giờ ra) → bản ghi ở trạng thái "Chờ duyệt".
- **Quy tắc:** chỉ chấm công cho ca đã được xếp trong ngày hôm đó; mỗi ca một bản ghi.

### UC021 — Duyệt chấm công *(cơ bản)*
- **Luồng chính:** quản lý xem danh sách chấm công chờ duyệt (lọc theo ngày/tháng/nhân viên) → bấm "Duyệt".
- **Quy tắc:** chỉ bản ghi đã duyệt mới được tính lương (BR-15).

### UC022 — Tính lương theo số ca trong tháng *(cơ bản)*
- **Luồng chính:** chọn tháng → hệ thống đếm số ca đã duyệt của từng nhân viên → nhân đơn giá ca → hiển thị bảng lương.
- **Quy tắc:** lương = tổng số ca đã duyệt × đơn giá ca (BR-14); không xử lý tăng ca, đi muộn, nghỉ phép.

### UC023 — Cho nhân viên nghỉ việc
- **Luồng chính:** chọn nhân viên → đổi trạng thái "đã nghỉ" → xóa khỏi lịch các tháng sau → ghi nhận trả dụng cụ/trang phục → khóa tài khoản đăng nhập → giữ lại dữ liệu lịch sử (không xóa cứng).

### UC027–UC032 — Nhóm báo cáo
- **Đầu vào chung:** khoảng thời gian (tháng/quý/năm), bộ lọc (ca, nhân viên, bàn, món).
- **Đầu ra chung:** bảng số liệu + tổng hợp + biểu đồ + xuất/in được.
- **Chi tiết:**
  - UC027: doanh thu theo tháng/quý/năm (từ hóa đơn đã thanh toán).
  - UC028: số khách theo ca (dùng số khách ghi trên hóa đơn + ca của hóa đơn).
  - UC029: số khách một nhân viên phục vụ trong tuần (theo nhân viên tiếp nhận).
  - UC030: số khách của bàn theo tháng/quý/năm (theo liên kết hóa đơn–bàn).
  - UC031: số lượng bán của từng món (tổng số lượng, doanh thu theo món).
  - UC032: số ca làm việc của nhân viên (từ lịch/chấm công đã duyệt).
- **Quy tắc:** số liệu phải khớp với dữ liệu gốc; không tính hóa đơn đã hủy/chưa thanh toán vào doanh thu.

## 3. Ma trận Use Case × Actor

| Use Case | Chủ quán | Quản lý | Phục vụ | Bếp |
|----------|:--------:|:-------:|:-------:|:---:|
| UC001–UC003 Xác thực | ✔ | ✔ | ✔ | ✔ |
| UC004–UC011 Bàn & đơn & thanh toán | ✔ | ✔ | ✔ | – |
| UC012–UC013 Bếp | – | – | – | ✔ |
| UC014–UC017, UC022–UC026 Quản trị | ✔ | – | – | – |
| UC018 Xem lịch của mình | ✔ | ✔ | ✔ | ✔ |
| UC019 Xem lịch chung | ✔ | ✔ | – | – |
| UC020 Chấm công | ✔ | ✔ | ✔ | ✔ |
| UC021 Duyệt chấm công | ✔ | ✔ | – | – |
| UC027–UC032 Báo cáo | ✔ | UC029, UC032 | – | – |
| UC033–UC034 Tùy chọn | ✔ | – | – | – |
