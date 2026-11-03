# Implementation Log — Phase 9: Kiểm thử nghiệp vụ có hỗ trợ Automation AI (2026-10-29)

## Phạm vi và trạng thái

Đối chiếu tĩnh service, SQL view và tài liệu yêu cầu/use case đã chỉ ra một số rủi ro tiềm ẩn có thể ảnh hưởng đến nghiệp vụ hoặc số liệu báo cáo. Các phát hiện ban đầu chưa được tái hiện bằng kiểm thử; các thay đổi dưới đây được thực hiện sau khi xác nhận quy tắc nghiệp vụ với người dùng.

**Trạng thái:** Đã triển khai các sửa đổi được xác định từ rà soát và chạy kiểm thử tích hợp trên SQL Server riêng; chưa chạy E2E giao diện trong giai đoạn này.

## Ghi nhận bổ sung — 29/10/2026

Lần chạy kiểm thử mở rộng phát hiện luồng chuyển bàn A → B → A bị lỗi do EF Core ánh xạ quan hệ `DiningTable`/`InvoiceTable` thành một-một, trong khi cơ sở dữ liệu lưu nhiều liên kết lịch sử cho cùng một bàn. Đã đổi navigation và mapping sang một-nhiều; chạy lại toàn bộ kiểm thử tích hợp đạt **8/8**.

## Phương pháp

- Đối chiếu `docs/02_BusinessRequirements.md`, `docs/04_UseCases.md` và `docs/06_DatabaseRequirements.md`.
- Đọc các luồng liên quan trong `OrderService`, `PaymentService`, `AttendanceService`, `ShiftService` và các view trong `database/03_views.sql`.
- Ghi nhận vị trí mã và hành vi cần dựng thành ca kiểm thử tự động/E2E.
- Quy tắc đã xác nhận: khi gộp hóa đơn, giữ số khách ban đầu trên từng bàn; khi duyệt chấm công, cho phép quản lý duyệt trước check-out; khi chuyển bàn, từ chối nếu bàn đích không đủ sức chứa số khách của bàn nguồn.

## Phát hiện ban đầu từ rà soát tĩnh

| Ưu tiên | Vị trí | Tình huống và ảnh hưởng cần xác minh |
|---------|--------|---------------------------------------|
| Cao — đã sửa | `OrderService.MoveTableAsync` | Không kiểm tra sức chứa bàn đích; đã bổ sung từ chối khi sức chứa nhỏ hơn số khách của bàn nguồn. |
| Cao — đã sửa | `OrderService.MergeInvoicesAsync`; `vw_CustomerByTable` | Tổng khách hóa đơn bị lặp cho từng bàn; đã thêm số khách trên liên kết hóa đơn-bàn và dùng số đó trong báo cáo. |
| Vừa — đã sửa | `PaymentService.GetReceiptAsync` | Liên kết bàn cũ sau chuyển có thể xuất hiện trên biên lai; biên lai hiện chỉ chọn bàn còn gắn lúc hóa đơn đóng. |
| Vừa — xác nhận là chủ ý | `AttendanceService.ApproveAsync`; `vw_ApprovedShiftCount` | Người dùng xác nhận quản lý được phép duyệt trước check-out; ca đã duyệt vẫn được tính theo BR-15. Kiểm thử khóa hành vi này. |
| Vừa — đã sửa | `ShiftService.ValidateAsync`, `SetActiveAsync`; gán ca trong `OrderService` | Ca đang hoạt động giờ không thể chồng giờ; kiểm tra cũng áp dụng khi sửa ca hoặc kích hoạt lại. |
| Thấp — đã sửa | `database/03_views.sql` — `vw_CustomerByEmployee` | Năm ISO và ngày đầu tuần được tính không nhất quán; view hiện dùng năm ISO và thứ Hai làm đầu tuần. |
| Bổ sung qua kiểm thử | `RestaurantDbContext` — quan hệ `DiningTable`/`InvoiceTable` | EF Core từng ánh xạ quan hệ lịch sử thành một-một, không khớp với dữ liệu cho phép nhiều liên kết hóa đơn-bàn theo thời gian; chuyển sang một-nhiều để hỗ trợ chuyển bàn quay lại bàn cũ. |

## Thay đổi sau khi chốt quy tắc

- Thêm `InvoiceTable.CustomerCount` để giữ số khách riêng theo từng bàn khi mở, chuyển và gộp hóa đơn; báo cáo khách theo bàn dùng số khách của liên kết bàn, không nhân lặp tổng hóa đơn.
- Báo cáo gộp các liên kết lặp của cùng một hóa đơn và bàn, tránh đếm lặp nếu khách chuyển đi rồi quay lại bàn cũ.
- Ánh xạ EF Core quan hệ giữa bàn và liên kết hóa đơn-bàn thành một-nhiều, phù hợp với lịch sử chuyển bàn được lưu trong cơ sở dữ liệu.
- Chuyển bàn kiểm tra số khách gắn với bàn nguồn so với sức chứa bàn đích. Dữ liệu cũ không xác định được số khách từng bàn sẽ bị chặn khi cần chuyển/gộp nhiều bàn, thay vì tự phân bổ sai.
- Biên lai chỉ liệt kê bàn còn gắn với hóa đơn sau khi chuyển.
- Duyệt chấm công trước check-out được giữ là luồng hợp lệ theo quyết định; ca đã duyệt tiếp tục được tính theo BR-15.
- Tuần báo cáo nhân viên tính `WeekStartDate` là thứ Hai và dùng năm ISO theo ngày thứ Năm của tuần.
- Tạo mới, cập nhật hoặc kích hoạt ca không cho phép trùng khung giờ với ca khác đang hoạt động; ca không hoạt động không tham gia kiểm tra.

## Kết quả kiểm thử thực thi

`.\database\run_automation_tests.ps1` provision database `RestaurantManagement_AutomationTest` riêng và chạy **8/8 kiểm thử tích hợp thành công** trên SQL Server. Bao phủ chuyển bàn vượt/đủ sức chứa, khách theo bàn sau khi gộp và chuyển quay lại bàn cũ, biên nhận, duyệt công trước check-out, ca chồng giờ và tuần ISO giao năm. Các test không dùng database demo. Chưa chạy kiểm thử E2E giao diện.

## Bước tiếp theo

Với database đã có dữ liệu từ trước khi thêm `InvoiceTable.CustomerCount`, số khách từng bàn của hóa đơn nhiều bàn không thể suy ra chắc chắn từ tổng hóa đơn; các dòng đó được để `NULL` thay vì phân bổ sai, và thao tác chuyển/gộp sẽ yêu cầu xử lý dữ liệu cũ trước.
