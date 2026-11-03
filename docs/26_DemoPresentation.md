# Kịch bản trình bày demo đồ án

**Đối tượng:** Giảng viên
**Thời lượng gợi ý:** 8–10 phút demo và 2–3 phút hỏi đáp
**Sản phẩm:** Hệ thống quản lý quán ăn — ASP.NET Core MVC, SQL Server, Docker

## 1. Mở đầu — 1 phút

> Em xin trình bày hệ thống quản lý quán ăn, tập trung vào quy trình phục vụ khách từ lúc mở bàn, gọi món, chế biến, thanh toán cho đến khi dữ liệu được tổng hợp trên báo cáo. Ngoài nghiệp vụ bán hàng, hệ thống còn hỗ trợ quản lý nhân viên, ca làm, chấm công và phân quyền theo vai trò.

> Mục tiêu của đồ án là thay thế việc theo dõi rời rạc bằng quy trình thống nhất; bảo đảm dữ liệu hóa đơn, bàn và món được cập nhật nhất quán, đồng thời giúp người quản lý tra cứu hoạt động và báo cáo.

## 2. Giới thiệu kiến trúc — 1 phút

> Ứng dụng được xây dựng trên ASP.NET Core MVC và .NET 10, dùng Entity Framework Core để làm việc với SQL Server 2022. Mã nguồn chia thành các tầng Web, Application, Infrastructure và Domain; SQL Server được khởi chạy bằng Docker Compose.

> Hệ thống có các vai trò như quản lý, thu ngân, phục vụ và bếp. Mỗi vai trò chỉ được truy cập những chức năng phù hợp. Em sẽ minh họa luồng bán hàng trước, sau đó xem kết quả ở báo cáo.

**Thao tác:** Mở trang đăng nhập; giới thiệu nhanh thanh điều hướng và các khu vực chức năng theo tài khoản demo. Tài khoản đăng nhập được ghi trong `README.md`.

## 3. Bán hàng: mở bàn và gọi món — 2 phút

> Tại màn hình Bán hàng (POS), nhân viên có thể xem trạng thái bàn để biết bàn nào đang trống hoặc đang phục vụ. Khi khách vào, nhân viên chọn bàn và nhập số khách. Hệ thống kiểm tra sức chứa để tránh nhận số khách vượt quá giới hạn của bàn.

**Thao tác:**

1. Chọn một bàn trống, mở bàn và nhập số khách hợp lệ.
2. Thêm một hoặc vài món, số lượng và ghi chú nếu cần.
3. Chỉ ra hóa đơn đang mở và trạng thái bàn được cập nhật.

> Giá món được lưu tại thời điểm gọi, để thay đổi giá thực đơn về sau không làm sai giá của đơn hàng đã phát sinh. Hệ thống cũng kiểm tra trạng thái món trước khi nhận gọi món.

## 4. Bếp xử lý món — 1 phút

> Sau khi gọi món, món xuất hiện trong hàng đợi bếp theo thứ tự thời gian. Nhân viên bếp có thể nhận chế biến rồi đánh dấu hoàn thành. Món đã được bếp nhận không thể bị hủy như món còn đang chờ; quy tắc này giúp hạn chế sai lệch giữa quầy và bếp.

**Thao tác:** Đăng nhập bằng vai trò bếp (hoặc dùng tài khoản có quyền bếp), mở hàng đợi, chuyển một món từ “Chờ” sang “Đang chế biến”, rồi sang “Hoàn thành”. Quay lại POS để cho thấy trạng thái đã cập nhật.

## 5. Thanh toán và biên nhận — 1–2 phút

> Khi khách yêu cầu thanh toán, hệ thống tổng hợp các món hợp lệ và hiển thị số tiền. Quản lý có thể áp dụng giảm giá theo quyền; phương thức thanh toán hỗ trợ tiền mặt hoặc chuyển khoản. Nếu vẫn còn món chưa hoàn thành, hệ thống cảnh báo và yêu cầu xác nhận trước khi đóng hóa đơn.

**Thao tác:**

1. Mở màn hình thanh toán của hóa đơn vừa tạo.
2. Chọn phương thức thanh toán; nếu minh họa giảm giá, dùng tài khoản có quyền quản lý.
3. Xác nhận thanh toán, mở biên nhận và chỉ ra tổng tiền, phương thức, các món cùng thao tác in.
4. Quay lại sơ đồ bàn, xác nhận bàn đã được giải phóng.

## 6. Báo cáo và kiểm soát — 1 phút

> Giao dịch vừa thực hiện có thể được đối chiếu trong báo cáo và nhật ký thao tác. Báo cáo hỗ trợ lọc dữ liệu và tải CSV; nhật ký hỗ trợ tra cứu các thay đổi quan trọng như chuyển hoặc gộp bàn.

**Thao tác:** Mở báo cáo, áp dụng một bộ lọc thời gian, chỉ ra số liệu liên quan đến hóa đơn demo; nếu thời gian cho phép, tải CSV hoặc mở nhật ký thao tác.

> Hệ thống còn có các chức năng quản lý nhân viên, ca làm, lịch làm và chấm công. Phần lương chỉ tổng hợp các ca đã được duyệt theo quy tắc nghiệp vụ.

## 7. Kiểm thử và kết luận — 1 phút

> Ngoài kiểm tra trực tiếp trên giao diện, đồ án có bộ kiểm thử tích hợp chạy trên SQL Server riêng. Bộ kiểm thử hiện có 8 ca, tập trung vào các tình huống như sức chứa khi chuyển bàn, giữ số khách theo bàn sau khi gộp, chuyển bàn quay lại bàn cũ, thanh toán, duyệt công và tuần báo cáo ISO. Lần chạy gần nhất đạt 8 trên 8.

> Qua phần demo, em đã trình bày luồng dữ liệu xuyên suốt từ POS đến bếp, thanh toán và báo cáo. Hướng tiếp theo là tiếp tục nghiệm thu các luồng trên giao diện và hoàn thiện dữ liệu demo, để việc kiểm tra đồ án được thuận tiện hơn.

## Chuẩn bị trước buổi trình bày

- Mở Docker Desktop và xác nhận SQL Server container đang chạy; cổng SQL Server là `1433`.
- Mở `src\RestaurantManagement.slnx` bằng Visual Studio, chọn `RestaurantManagement.Web` làm Startup Project và chọn profile `http`.
- Chạy ứng dụng bằng **F5**; xác nhận trang đăng nhập truy cập được tại `http://localhost:5010` và kiểm tra `/health/db`.
- Đọc trước hướng dẫn khởi tạo database và tài khoản demo trong `README.md`; đăng nhập thử các vai trò cần dùng.
- Chuẩn bị sẵn món đang hoạt động, bàn trống đủ sức chứa và tài khoản có quyền bếp. Nếu demo trên dữ liệu dùng chung, không chạy lại thao tác khởi tạo hoặc xóa Docker volume vì có thể mất dữ liệu.
- Mở sẵn các trang POS, hàng đợi bếp, thanh toán, báo cáo và nhật ký nếu cần tiết kiệm thời gian khi chuyển vai trò.
- Nên dùng dữ liệu demo giả lập; không đưa thông tin cá nhân hoặc dữ liệu thật vào buổi trình bày.

## Phương án dự phòng

- Nếu một món không xuất hiện trong hàng đợi bếp, kiểm tra trạng thái món và hóa đơn đang mở; không tiếp tục thanh toán cho đến khi xác định được nguyên nhân.
- Nếu báo cáo chưa có dữ liệu, xác nhận hóa đơn đã thanh toán và bộ lọc thời gian phù hợp. Có thể minh họa bằng CSV hoặc dữ liệu seed thay vì tạo giao dịch thật trong môi trường dùng chung.
- Nếu không thể chuyển tài khoản trong thời gian demo, tập trung trình bày luồng POS → thanh toán bằng vai trò quản lý; giải thích phân quyền bếp và minh họa bằng bộ kiểm thử tích hợp.
- Không khẳng định một luồng giao diện đã nghiệm thu nếu chưa chạy thử trên đúng phiên bản sẽ dùng trong buổi báo cáo. Kết quả 8/8 ở trên là kiểm thử tích hợp nghiệp vụ, không thay thế kiểm thử E2E giao diện.

## Câu hỏi giảng viên có thể đặt

| Câu hỏi | Ý trả lời ngắn |
|---|---|
| Vì sao chia thành nhiều tầng? | Để tách giao diện, quy tắc nghiệp vụ, truy cập dữ liệu và mô hình miền; giảm phụ thuộc giữa các phần và thuận tiện kiểm thử. |
| Làm sao bảo đảm giá hóa đơn không đổi khi sửa giá món? | Ghi nhận giá món vào chi tiết hóa đơn tại thời điểm gọi món. |
| Vì sao dùng cả kiểm tra ở ứng dụng và cơ sở dữ liệu? | Service đưa thông báo nghiệp vụ dễ hiểu; ràng buộc/trigger ở database là lớp bảo vệ bổ sung cho dữ liệu. |
| Kiểm thử tự động đã chứng minh điều gì? | 8 ca tích hợp xác minh một số quy tắc trọng yếu trực tiếp với SQL Server; không đại diện cho toàn bộ kiểm thử giao diện hay mọi tình huống sử dụng. |
| Dự án có sẵn sàng production chưa? | Đây là cấu hình đồ án/demo cục bộ; không nên xem là cấu hình production. Cần rà soát thêm vận hành, tài khoản, dữ liệu, sao lưu và bảo mật trước khi triển khai thật. |
