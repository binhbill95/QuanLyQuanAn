# Implementation Log — Phase 8: Báo cáo (2026-10-28)

## Phạm vi

Triển khai UC027–UC032 trên các view báo cáo đã có trong CSDL, không thay đổi schema. Dành cho ADMIN, OWNER và MANAGER theo ma trận quyền.

**Trạng thái:** Hoàn tất triển khai, build, kiểm thử HTTP và kiểm tra UI trình duyệt.

## Kết quả

- Dashboard `/Reports` gồm doanh thu theo tháng; khách theo ca, nhân viên/tuần và bàn; số lượng/doanh thu theo món; số ca đã duyệt và lương theo tháng.
- Bộ lọc chung gồm năm, tháng, quý, tuần, ca, nhân viên, bàn và món; các lựa chọn được lấy từ dữ liệu thực.
- Thẻ KPI tổng hợp doanh thu, hóa đơn, khách và lương; bảng doanh thu có biểu đồ thanh tỷ lệ.
- Hỗ trợ in dashboard và tải CSV UTF-8 cho cả sáu báo cáo.
- Dùng `vw_RevenueByPeriod`, `vw_CustomerByShift`, `vw_CustomerByEmployee`, `vw_CustomerByTable`, `vw_ItemSales`, `vw_Payroll`; các view chỉ tính hóa đơn đã thanh toán, công đã duyệt.
- Khi mở hóa đơn, tự gắn ca đang hoạt động theo giờ địa phương hiện tại để báo cáo khách theo ca có dữ liệu; ngoài giờ ca, hóa đơn vẫn mở với `ShiftId` để trống. Nếu dữ liệu ca bị chồng giờ, thao tác bị chặn thay vì gán ca tùy ý.

## Kiểm chứng

| Kiểm tra | Kết quả |
|----------|---------|
| `dotnet build src\RestaurantManagement.slnx --no-restore` | Thành công, 0 warning / 0 error |
| MANAGER mở `/Reports` và áp dụng bộ lọc năm/tháng/quý/tuần | HTTP 200 |
| ADMIN mở dashboard và tải CSV | Được phép theo policy Management |
| Export CSV | HTTP 200, `text/csv`, có đủ sáu nhóm dữ liệu |
| Năm ngoài miền hợp lệ khi export | Bị từ chối HTTP 400 |
| Thanh toán hóa đơn thử 2 khách, 2 phần M001 | Các view lần lượt ghi nhận doanh thu 90.000, khách theo ca/nhân viên/bàn, số lượng món 2; dữ liệu CSV khớp |
| Các view sau khi dọn E2E | Dữ liệu giao dịch và thanh toán thử đã được xóa; công/lương hiện không có dữ liệu chấm công đã duyệt |
| Kiểm tra UI bằng trình duyệt với MANAGER | `/Reports` và `/Payments` tải được; trạng thái rỗng phù hợp với CSDL sạch |
| Điều hướng tại viewport 879px | Nút hamburger đóng/mở menu đúng; không còn cuộn ngang |
| Điều hướng sau khi nhóm menu | Trang chủ giữ lối tắt riêng; nhóm Tác nghiệp và Quản lý mở đúng các liên kết theo quyền MANAGER |

## Ghi chú triển khai

- Các báo cáo dùng nguồn dữ liệu/view database-first hiện hữu; không tái tạo logic tổng hợp trong tầng web.
- `vw_CustomerByShift` phụ thuộc `Invoice.ShiftId`; Phase 6 trước đây chưa gán giá trị này khi mở hóa đơn, nên luồng mở hóa đơn đã được cập nhật để chụp ca hiện hành từ các ca đang sử dụng.
- Doanh thu/khách/món nhóm theo thời điểm đóng hóa đơn; công/lương theo tháng làm việc và chỉ chấm công đã duyệt.
- CSV dùng UTF-8 BOM, định dạng số bất biến và escape chuỗi có tiền tố công thức để hạn chế CSV formula injection.
- CSDL hiện không có dữ liệu lịch sử để đối chiếu; báo cáo công/lương đang ở trạng thái rỗng cho tới khi phát sinh chấm công đã duyệt.
- Kiểm tra UI phát hiện breakpoint của `navbar-expand-xxl` chưa khớp với tiện ích `d-sm-inline-flex`, khiến menu bung thành cột trên màn hình hẹp. Đã đổi thành `d-xxl-inline-flex`; xác minh menu ẩn mặc định, hiện khi bấm hamburger và không có overflow ngang.
- Sau phản hồi UI, gom các đường dẫn dài thành hai menu thả xuống “Tác nghiệp” và “Quản lý”, giữ “Trang chủ” làm lối tắt, và dùng breakpoint `xl` để điều hướng bung trên màn desktop rộng. Các liên kết tiếp tục tuân theo điều kiện role hiện có; đã kiểm tra menu quản lý và các mục của MANAGER trên trình duyệt.
