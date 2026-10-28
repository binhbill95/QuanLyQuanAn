# Biểu đồ tuần tự — Hệ thống quản lý quán ăn

Tài liệu mô tả các luồng tương tác chính giữa người dùng, giao diện MVC, controller, service và SQL Server. Các biểu đồ được viết bằng Mermaid; có thể xem trực tiếp trên GitHub hoặc trong trình soạn thảo hỗ trợ Mermaid.

## 1. Đăng nhập

Luồng đăng nhập xác thực tài khoản hoạt động, kiểm tra mật khẩu hash, sau đó tạo cookie chứa các claim cần thiết. Những request đã đăng nhập tiếp tục được kiểm tra trạng thái tài khoản để vô hiệu hóa phiên nếu tài khoản bị khóa, đổi vai trò hoặc nhân viên nghỉ việc.

```mermaid
sequenceDiagram
    autonumber
    actor User as Nhân viên
    participant Browser as Trình duyệt
    participant Controller as AccountController
    participant Auth as UserAuthenticationService
    participant DB as SQL Server
    participant Cookie as Cookie Authentication

    User->>Browser: Nhập tên đăng nhập và mật khẩu
    Browser->>Controller: POST /Account/Login
    Controller->>Auth: ValidateCredentialsAsync(username, password)
    Auth->>DB: Tìm AppUser, Role, Employee
    DB-->>Auth: Thông tin tài khoản
    Auth->>Auth: Kiểm tra IsActive và PasswordHash
    alt Thông tin hợp lệ
        Auth->>DB: Cập nhật LastLoginAt
        Auth-->>Controller: AuthenticatedUser
        Controller->>Cookie: SignInAsync (userId, role, employeeId, auth_stamp)
        Cookie-->>Browser: Set-Cookie
        Controller-->>Browser: Redirect về ReturnUrl hợp lệ hoặc Home
    else Sai mật khẩu, không tồn tại hoặc tài khoản bị khóa
        Auth-->>Controller: null
        Controller-->>Browser: Hiển thị thông báo lỗi chung
    end

    Note over Browser,Cookie: Cookie được kiểm tra lại với trạng thái tài khoản trên request tiếp theo.
```

## 2. Mở bàn và gọi món

Mở bàn kiểm tra nhân viên, trạng thái bàn, số ghế và hóa đơn đang hoạt động trong transaction `Serializable`. Mỗi lần gọi món tạo một dòng chi tiết mới, chụp giá hiện tại vào hóa đơn và xếp món vào hàng đợi bếp.

```mermaid
sequenceDiagram
    autonumber
    actor Waiter as Phục vụ / Quản lý
    participant Browser as Trình duyệt POS
    participant Orders as OrdersController
    participant Service as OrderService
    participant DB as SQL Server
    participant Audit as Trigger Audit

    Waiter->>Browser: Chọn bàn, nhập số khách
    Browser->>Orders: POST /Orders/Open
    Orders->>Service: OpenInvoiceAsync(tableId, customerCount, employeeId)
    Service->>DB: BEGIN TRANSACTION Serializable
    Service->>DB: Kiểm tra nhân viên, bàn, sức chứa và bàn đang bận
    alt Bàn hợp lệ và còn trống
        Service->>DB: Tạo Invoice và InvoiceTable
        Service->>DB: SaveChanges
        DB->>Audit: Ghi thay đổi Invoice
        DB-->>Service: InvoiceId
        Service->>DB: COMMIT
        Service-->>Orders: Thành công (InvoiceId)
        Orders-->>Browser: Mở hóa đơn đang phục vụ
    else Nhân viên/bàn không hợp lệ, quá sức chứa hoặc bàn đã có khách
        Service->>DB: Hoàn tất transaction (không có thay đổi)
        Service-->>Orders: Lỗi nghiệp vụ
        Orders-->>Browser: Thông báo lỗi
    end

    Waiter->>Browser: Chọn món, số lượng, ghi chú
    Browser->>Orders: POST /Orders/AddItem
    Orders->>Service: AddItemAsync(invoiceId, menuItemId, quantity, note)
    Service->>DB: Kiểm tra hóa đơn đang mở
    Service->>DB: Kiểm tra món chưa xóa, còn bán và nhóm đang hoạt động
    alt Hóa đơn và món hợp lệ
        Service->>DB: Thêm InvoiceDetail (UnitPrice = giá hiện tại, Status = chờ)
        Service->>DB: SaveChanges
        DB->>Audit: Ghi thay đổi Invoice và InvoiceDetail
        DB-->>Service: Lưu thành công
        Service-->>Orders: Thành công
        Orders-->>Browser: Hiển thị món trong hóa đơn
    else Hóa đơn đóng hoặc món hết/ngừng/không tồn tại
        Service-->>Orders: Lỗi nghiệp vụ
        Orders-->>Browser: Thông báo, không thêm món
    end
```

## 3. Bếp nhận và hoàn thành món

Hàng đợi lấy các món đang phục vụ từ view `vw_KitchenQueue`, theo thứ tự thời gian đặt (FIFO). Trạng thái chỉ chuyển theo thứ tự `Chờ → Đang chế biến → Hoàn thành`.

```mermaid
sequenceDiagram
    autonumber
    actor Kitchen as Nhân viên bếp
    participant Browser as Trình duyệt
    participant Controller as KitchenController
    participant Service as OrderService
    participant DB as SQL Server
    participant Audit as Trigger Audit

    Kitchen->>Browser: Mở hàng đợi bếp
    Browser->>Controller: GET /Kitchen/Queue
    Controller->>Service: GetKitchenQueueAsync()
    Service->>DB: Đọc vw_KitchenQueue, sắp OrderedAt rồi InvoiceDetailId
    DB-->>Service: Danh sách món đang chờ / chế biến
    Service-->>Controller: KitchenQueue
    Controller-->>Browser: Hiển thị bàn, món, số lượng, ghi chú

    Kitchen->>Browser: Nhận chế biến món
    Browser->>Controller: POST /Kitchen/SetStatus (status = 1)
    Controller->>Service: SetKitchenStatusAsync(detailId, 1)
    Service->>DB: Kiểm tra hóa đơn mở và món đang chờ
    alt Món đang chờ
        Service->>DB: Cập nhật Status = đang chế biến
        Service->>DB: SaveChanges
        DB->>Audit: Ghi thay đổi InvoiceDetail
        Service-->>Controller: Thành công
    else Món không còn chờ
        Service-->>Controller: Lỗi trạng thái
    end

    Kitchen->>Browser: Đánh dấu hoàn thành
    Browser->>Controller: POST /Kitchen/SetStatus (status = 2)
    Controller->>Service: SetKitchenStatusAsync(detailId, 2)
    Service->>DB: Chỉ chấp nhận món đang chế biến
    DB-->>Service: Lưu Status = hoàn thành và CompletedAt
    Service-->>Controller: Kết quả
    Controller-->>Browser: Cập nhật hàng đợi
```

## 4. Chuyển bàn và gộp hóa đơn

Hai thao tác được thực hiện trong transaction `Serializable`. Chuyển bàn yêu cầu bàn đích trống. Gộp hóa đơn chuyển các bàn và chi tiết món sang hóa đơn nhận, ghi lịch sử chuyển/gộp và kiểm tra sức chứa cộng dồn.

```mermaid
sequenceDiagram
    autonumber
    actor Waiter as Phục vụ / Quản lý
    participant Browser as Trình duyệt POS
    participant Controller as OrdersController
    participant Service as OrderService
    participant DB as SQL Server
    participant Audit as Trigger Audit

    alt Chuyển bàn
        Waiter->>Browser: Chọn bàn đích
        Browser->>Controller: POST /Orders/MoveTable
        Controller->>Service: MoveTableAsync(invoiceId, fromTableId, toTableId, employeeId)
        Service->>DB: BEGIN TRANSACTION Serializable
        Service->>DB: Xác nhận hóa đơn mở, bàn nguồn thuộc hóa đơn, bàn đích hoạt động và trống
        alt Điều kiện hợp lệ
            Service->>DB: Giải phóng liên kết bàn nguồn, gắn bàn đích
            Service->>DB: Thêm TableTransferLog
            Service->>DB: SaveChanges
            DB->>Audit: Ghi các thay đổi được audit
            Service->>DB: COMMIT
            Service-->>Controller: Thành công
        else Có lỗi điều kiện
            Service->>DB: Hoàn tất transaction (không có thay đổi)
            Service-->>Controller: Lỗi nghiệp vụ
        end
    else Gộp hóa đơn
        Waiter->>Browser: Chọn hóa đơn nhận và hóa đơn cần gộp
        Browser->>Controller: POST /Orders/Merge
        Controller->>Service: MergeInvoicesAsync(targetId, sourceId, employeeId)
        Service->>DB: BEGIN TRANSACTION Serializable
        Service->>DB: Xác nhận hai hóa đơn mở và có bàn đang phục vụ
        Service->>DB: Kiểm tra tổng khách không vượt sức chứa các bàn
        alt Điều kiện hợp lệ
            Service->>DB: Chuyển InvoiceTable và InvoiceDetail sang hóa đơn nhận
            Service->>DB: Đóng hóa đơn nguồn, cộng số khách vào hóa đơn nhận
            Service->>DB: Thêm TableTransferLog
            Service->>DB: SaveChanges
            DB->>Audit: Ghi thay đổi hóa đơn và chi tiết
            Service->>DB: COMMIT
            Service-->>Controller: Thành công
        else Hóa đơn không hợp lệ hoặc quá sức chứa
            Service->>DB: Hoàn tất transaction (không có thay đổi)
            Service-->>Controller: Lỗi nghiệp vụ
        end
    end
    Controller-->>Browser: Thông báo kết quả và tải lại hóa đơn
```

## 5. Thanh toán và tạo biên nhận

Hệ thống tính tổng từ chi tiết món chưa hủy; giảm giá chỉ dành cho MANAGER và không vượt tiền món. Nếu còn món chưa hoàn thành, người dùng phải xác nhận. Việc đóng hóa đơn, ghi thanh toán và giải phóng bàn nằm trong cùng transaction `Serializable`.

```mermaid
sequenceDiagram
    autonumber
    actor Cashier as Thu ngân / Quản lý
    participant Browser as Trình duyệt
    participant Controller as PaymentsController
    participant Service as PaymentService
    participant DB as SQL Server
    participant Audit as Trigger Audit

    Cashier->>Browser: Chọn hóa đơn cần thanh toán
    Browser->>Controller: GET /Payments/Checkout
    Controller->>Service: GetCheckoutAsync(invoiceId)
    Service->>DB: Đọc hóa đơn mở, chi tiết món và các bàn
    DB-->>Service: Tạm tính, món chưa hoàn thành
    Service-->>Controller: Checkout
    Controller-->>Browser: Hiển thị tổng tiền, giảm giá và phương thức

    Cashier->>Browser: Xác nhận thanh toán
    Browser->>Controller: POST /Payments/Pay
    Controller->>Service: PayAsync(invoiceId, method, discount, confirmation, employeeId)
    Service->>DB: BEGIN TRANSACTION Serializable
    Service->>DB: Kiểm tra nhân viên, hóa đơn mở và chưa thanh toán
    Service->>DB: Tính tổng món chưa hủy; xác thực giảm giá
    alt Còn món chưa hoàn thành và chưa xác nhận
        Service->>DB: Hoàn tất transaction (không có thay đổi)
        Service-->>Controller: Yêu cầu xác nhận
    else Dữ liệu hợp lệ
        Service->>DB: Ghi DiscountAmount, đóng Invoice, giải phóng các bàn
        Service->>DB: Thêm Payment (tổng sau giảm giá)
        Service->>DB: SaveChanges
        DB->>Audit: Ghi thay đổi Invoice và Payment
        Service->>DB: COMMIT
        Service-->>Controller: Thành công (invoiceId)
        Controller-->>Browser: Redirect đến Receipt
        Browser->>Controller: GET /Payments/Receipt
        Controller->>Service: GetReceiptAsync(invoiceId)
        Service->>DB: Đọc hóa đơn đã thanh toán và Payment
        DB-->>Service: Dữ liệu biên nhận
        Service-->>Controller: Receipt
        Controller-->>Browser: Hiển thị biên nhận để in
    else Hóa đơn không hợp lệ, giảm giá sai hoặc không còn bàn
        Service->>DB: Hoàn tất transaction (không có thay đổi)
        Service-->>Controller: Lỗi nghiệp vụ
        Controller-->>Browser: Thông báo lỗi thanh toán
    end
```

## 6. Chấm công và duyệt công

Nhân viên chỉ check-in lịch ca của chính mình trong ngày hiện tại; check-out gắn vào bản ghi đã tạo. Quản lý duyệt bản ghi để xác nhận công; báo cáo lương chỉ sử dụng công đã duyệt.

```mermaid
sequenceDiagram
    autonumber
    actor Employee as Nhân viên
    actor Manager as Quản lý
    participant Browser as Trình duyệt
    participant Controller as AttendanceController
    participant Service as AttendanceService
    participant DB as SQL Server
    participant Audit as Trigger Audit

    Employee->>Browser: Check-in ca hôm nay
    Browser->>Controller: POST /Attendance/CheckIn (scheduleId)
    Controller->>Service: CheckInAsync(scheduleId, currentEmployeeId)
    Service->>DB: Đọc lịch ca
    Service->>DB: Kiểm tra chủ sở hữu, ngày hiện tại và chưa chấm công
    alt Lịch hợp lệ
        Service->>DB: Tạo Attendance (CheckInAt, Status = chờ duyệt)
        Service->>DB: SaveChanges
        DB->>Audit: Ghi thay đổi Attendance
        Service-->>Controller: Thành công (attendanceId)
    else Không phải lịch của nhân viên, sai ngày hoặc đã check-in
        Service-->>Controller: Lỗi nghiệp vụ
    end
    Controller-->>Browser: Hiển thị kết quả

    Employee->>Browser: Check-out
    Browser->>Controller: POST /Attendance/CheckOut (attendanceId)
    Controller->>Service: CheckOutAsync(attendanceId, currentEmployeeId)
    Service->>DB: Kiểm tra bản ghi thuộc nhân viên và chưa check-out
    Service->>DB: Lưu CheckOutAt
    DB->>Audit: Ghi thay đổi Attendance
    Service-->>Controller: Kết quả
    Controller-->>Browser: Hiển thị kết quả

    Manager->>Browser: Duyệt bản ghi chấm công
    Browser->>Controller: POST /Attendance/Approve
    Controller->>Service: ApproveAsync(attendanceId, approverEmployeeId)
    Service->>DB: Cập nhật Status = đã duyệt, ApprovedBy, ApprovedAt
    DB->>Audit: Ghi thay đổi Attendance
    Service-->>Controller: Kết quả
    Controller-->>Browser: Thông báo đã duyệt
```

## Ghi chú thiết kế

- Các sơ đồ mô tả luồng chính, không bao gồm mọi nhánh lỗi hoặc toàn bộ thao tác quản trị CRUD.
- Controller chịu trách nhiệm nhận request, kiểm tra quyền/ModelState và gọi service; quy tắc nghiệp vụ nằm trong service, dữ liệu được lưu qua EF Core.
- Các thao tác ghi lên bảng có trigger audit đặt `ChangedByUserId` qua `sys.sp_set_session_context` ngay trong lần `SaveChanges`; trigger SQL Server ghi nội dung thay đổi vào `AuditLog`.
- Thời điểm ghi trong database dùng UTC; các trang nghiệp vụ hiển thị giờ Việt Nam.
- Những thao tác có tính cạnh tranh bàn/hóa đơn sử dụng transaction; mở bàn, chuyển bàn, gộp hóa đơn và thanh toán dùng `Serializable`.
- Nếu service trả lỗi nghiệp vụ trước khi ghi, transaction hoàn tất mà không có thay đổi dữ liệu.

## Tài liệu liên quan

- Ca sử dụng: `04_UseCases.md`
- Yêu cầu nghiệp vụ: `02_BusinessRequirements.md`
- Thiết kế database: `11_DatabaseSchemaDesign.md`
- Cài đặt và chạy ứng dụng: `../README.md`
