# Đánh giá bảo mật cho bản demo (2026-10-29)

## Phạm vi và kết luận

Đây là đồ án demo chạy cục bộ, sử dụng ASP.NET Core MVC trên .NET 10, EF Core và SQL Server 2022 trong Docker; không sử dụng PHP. Kiến trúc có các biện pháp nền tảng như băm mật khẩu, cookie xác thực `HttpOnly`/`SameSite=Lax`, phân quyền theo vai trò, antiforgery cho các POST và truy vấn dữ liệu qua EF Core.

Các biện pháp trên không đồng nghĩa ứng dụng đã được kiểm thử hoặc gia cố cho Internet/production. Đánh giá này tập trung vào cấu hình và mã nguồn hiện có, không phải kiểm thử xâm nhập hay chứng nhận an ninh.

## Phát hiện và cách xử lý

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | 🟠 HIGH | `database/02_seed.sql` | 7-8, 113-122 | Seed tạo tài khoản `admin` và `manager` hoạt động với mật khẩu demo cố định `Admin@123`; tài khoản admin có quyền quản trị. | 9/10 |
| 2 | 🟡 MEDIUM | `docker/docker-compose.yml` | 21-22 | Cấu hình cũ publish cổng SQL Server trên mọi giao diện mạng của host, có thể làm lộ dịch vụ nếu máy nằm trên mạng không tin cậy. | 8/10 |

### Quyết định phù hợp với mục tiêu chấm đồ án

- Giữ tài khoản mẫu trong `02_seed.sql` để giảng viên có thể khởi tạo database và đăng nhập kiểm tra ngay.
- Chỉ dùng các tài khoản đó cho database demo cục bộ; không dùng dữ liệu cá nhân/thật và không triển khai database seed ra môi trường công khai.
- Cổng SQL Server hiện bind vào `127.0.0.1` trên host. Không đổi bind thành `0.0.0.0`, không cấu hình port-forward/router/firewall cho cổng 1433.
- Trang đăng nhập chỉ hiện hướng dẫn tài khoản demo trong môi trường `Development`, không hiển thị thông tin đó trong môi trường `Production`.
- Cấu hình ứng dụng demo vẫn dùng tài khoản SQL `sa` trong hướng dẫn cục bộ. Đây là lựa chọn đơn giản cho đồ án, không phải nguyên tắc least-privilege cho môi trường production.
- README và hướng dẫn Docker đã ghi rõ giới hạn demo và cảnh báo không dùng cấu hình này cho production.

## Rủi ro còn lại và khuyến nghị nếu triển khai thật

Tài khoản và mật khẩu mẫu vẫn được lưu trong script seed công khai; người tải repo có thể đọc chúng và database khởi tạo mới sẽ tạo tài khoản đó. Việc ẩn chúng khỏi giao diện Production không vô hiệu hóa tài khoản ở database. Vì vậy không đưa database demo hoặc ứng dụng dùng database demo lên Internet.

Trước khi xây dựng môi trường production, cần có quy trình cấp tài khoản/mật khẩu riêng cho từng môi trường, loại bỏ hoặc vô hiệu hóa tài khoản demo, tạo SQL login riêng với quyền tối thiểu thay cho `sa`, quản lý secrets ngoài mã nguồn, bật HTTPS và cookie secure, giới hạn mạng tới database, cùng kiểm thử bảo mật phù hợp. Các hạng mục production này chưa thuộc phạm vi đồ án demo.

## Tài liệu và cấu hình liên quan

- Hướng dẫn demo và cảnh báo: `../README.md`
- Cấu hình Docker SQL Server: `../docker/docker-compose.yml`
- Hướng dẫn chạy SQL Server: `../docker/README.md`
- Tài khoản demo được tạo tại: `../database/02_seed.sql`
