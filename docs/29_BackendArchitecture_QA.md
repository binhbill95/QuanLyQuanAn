# Cẩm nang trả lời giảng viên — Kiến trúc backend, API và phần chưa có giao diện

> Tài liệu dùng khi bảo vệ/demo đồ án. Mục tiêu: trả lời ngắn, đúng thuật ngữ, chỉ được file chứng minh trong repo và không nói quá phạm vi đã làm.

- **Dự án:** Restaurant Management System
- **Công nghệ:** .NET 10, ASP.NET Core MVC, EF Core 10, SQL Server 2022
- **Kiểu kiến trúc:** monolith theo hướng Clean Architecture
- **Frontend:** Razor Views + Bootstrap 5 + jQuery validation (server-rendered, không phải SPA)
- **Cập nhật:** 2026-11-13

---

## 1. Câu chốt 30 giây

Nếu giảng viên hỏi tổng quan, trả lời:

> Đồ án là ứng dụng monolith ASP.NET Core MVC theo hướng Clean Architecture, gồm 4 tầng: Domain, Application, Infrastructure và Web. Domain chứa entity không phụ thuộc project nào; Application chứa DTO, interface service và kiểu kết quả; Infrastructure cài đặt service bằng EF Core và làm việc với SQL Server; Web là composition root, nhận request MVC, gọi service và render Razor View. Hiện tại hệ thống **không có REST API công khai**, giao diện web gọi trực tiếp controller MVC; hợp đồng nội bộ giữa các tầng là các interface service trong tầng Application.

Nếu chỉ được nói một câu:

> **Đây là MVC server-rendered, không phải SPA + REST API; backend được chia tầng theo Clean Architecture, còn frontend là Razor View.**

---

## 2. Kiến trúc backend

### 2.1. Sơ đồ phụ thuộc

```text
Web (ASP.NET Core MVC)
  │  Controller, ViewModel, Razor View, CurrentUserService
  ▼
Infrastructure (EF Core, service implementations)
  │  DbContext, business services, authentication, DI extension
  ▼
Application (contracts)
  │  DTO, interface service, OperationResult, PagedResult
  ▼
Domain (entities)
     Entity scaffold từ database, không phụ thuộc tầng nào
```

Quan hệ project thực tế:

- `Web` → `Infrastructure`, `Application`, `Domain`
- `Infrastructure` → `Application`, `Domain`
- `Application` → `Domain`
- `Domain` → không tham chiếu project nào
- `Tests` → `Infrastructure`

**Điểm cần nhấn:** chiều phụ thuộc đi từ ngoài vào trong; Domain không biết EF Core, Web hay SQL Server.

### 2.2. Trách nhiệm từng tầng

| Tầng | Project | Chứa gì | Không chứa gì |
|------|---------|---------|----------------|
| Domain | `RestaurantManagement.Domain` | 26 entity bảng + 10 entity view, enum/kiểu dữ liệu cốt lõi | EF Core, controller, DTO trình bày |
| Application | `RestaurantManagement.Application` | 16 interface service nghiệp vụ, DTO, `OperationResult<T>`, `PagedResult<T>`, `ICurrentUserService` | `DbContext`, SQL, Razor, `IHttpContextAccessor` |
| Infrastructure | `RestaurantManagement.Infrastructure` | `RestaurantDbContext`, cài đặt service, truy vấn EF Core, transaction, xác thực, audit, DI extension | Razor View, `Controller`, `TempData` |
| Web | `RestaurantManagement.Web` | Controller, ViewModel, Razor View, `CurrentUserService`, `Program.cs`, cấu hình cookie/policy | Câu lệnh SQL trực tiếp, business rule phức tạp |
| Tests | `RestaurantManagement.Tests` | Kiểm thử tích hợp nghiệp vụ trên SQL Server riêng | Test UI/E2E |

### 2.3. Luồng xử lý một request

Ví dụ `POST /Orders/Open`:

```text
Browser (form + antiforgery token)
  → OrdersController.Open(...)
    → kiểm tra đăng nhập/quyền, ModelState, lấy employee hiện tại
    → IOrderService.OpenInvoiceAsync(...)
      → OrderService: kiểm tra bàn, sức chứa, hóa đơn đang mở
      → mở transaction Serializable
      → tạo Invoice + InvoiceTable qua EF Core
      → SaveChangesAsync()
      → commit
    → trả OperationResult<int>
  → Controller đặt TempData thông báo
  → RedirectToAction (Post/Redirect/Get)
  → Razor View render HTML
```

Điểm quan trọng để nói:

- **Controller mỏng:** chỉ nhận request, kiểm tra `ModelState`, phân quyền, gọi service, đặt `TempData`, redirect.
- **Business rule nằm trong service:** ví dụ sức chứa bàn, trạng thái món, chuyển/gộp bàn, tính tiền.
- **Truy vấn dữ liệu bằng EF Core:** projection sang DTO, `AsNoTracking()` cho truy vấn đọc.
- **Lỗi nghiệp vụ không dùng exception:** service trả `OperationResult<T>` với `Succeeded` và `Error`.
- **Ghi dữ liệu có transaction:** mở bàn, chuyển bàn, gộp hóa đơn và thanh toán dùng `IsolationLevel.Serializable`.

### 2.4. Các mẫu thiết kế đang dùng

| Chủ đề | Cách làm | File chứng minh |
|--------|----------|-----------------|
| Composition root | `Program.cs` gọi `AddInfrastructure`, đăng ký cookie auth, policy, `ICurrentUserService` | `Web/Program.cs` |
| DI cho tầng dữ liệu | `AddDbContext`, `AddScoped` cho từng service | `Infrastructure/DependencyInjection.cs` |
| Tách DTO và ViewModel | DTO ở Application; ViewModel chỉ phục vụ Razor | `Application/Models`, `Web/Models` |
| Kết quả nghiệp vụ | `OperationResult<T>` thay cho exception | `Application/Common` |
| Truy vấn đọc | `AsNoTracking` + `Select` sang DTO | các service Infrastructure |
| Báo cáo | SQL View + entity keyless | `database/03_views.sql`, `RestaurantDbContext` |
| Phân tách chi nhánh | Claim `branch_id` + global query filter | `CurrentUserService`, `RestaurantDbContext` |
| Audit | Trigger SQL Server + `SESSION_CONTEXT` | `database/04_audit.sql`, `AuditService` |
| Đồng bộ schema | Database-first, script SQL là nguồn sự thật | `database/*.sql` |

### 2.5. Điểm cần nói thật về Clean Architecture

Nếu giảng viên soi kỹ, **không nên nói “sạch hoàn hảo”**. Cách trả lời trung thực:

> Mô hình đạt được chiều phụ thuộc và tách được Domain/Application khỏi hạ tầng, nhưng phần cài đặt use case hiện đặt trong Infrastructure vì dùng trực tiếp EF Core. Nếu muốn “Clean” triệt để hơn, có thể chuyển các use case thuần vào Application và để Infrastructure chỉ làm adapter cho repository/DB. Đây là hướng cải tiến, không phải lỗi chức năng.

Lý do hợp lý cho lựa chọn hiện tại:

- Đồ án nhỏ, một team; thêm repository/use-case layer sẽ tăng boilerplate.
- EF Core `DbContext` đã đóng vai trò Unit of Work + repository.
- Service interface ở Application vẫn tạo ranh giới để thay implementation hoặc viết test.

---

## 3. API

### 3.1. Kết luận quan trọng

> **Đồ án hiện tại không có REST API, không có Swagger/OpenAPI, không dùng JWT.**  
> Giao diện web giao tiếp với backend qua **các route MVC**: GET trả HTML, POST nhận form-urlencoded và trả redirect/HTML.

Không nói “em có API REST” nếu giảng viên đang hỏi JSON/Swagger, vì sẽ bị hỏi ngược và làm mất điểm.

Có thể phân biệt 2 nghĩa của “API”:

1. **API hướng ra ngoài (HTTP API):** hiện chỉ có các endpoint MVC và một endpoint health JSON.
2. **API nội bộ giữa các tầng:** 16 interface service trong `Application/Abstractions` là hợp đồng nghiệp vụ; Infrastructure cài đặt các interface này.

### 3.2. API nội bộ — interface service

| Nhóm nghiệp vụ | Interface |
|----------------|-----------|
| Xác thực | `IUserAuthenticationService` |
| Tài khoản | `IAccountService` |
| Nhân viên | `IEmployeeService` |
| Chi nhánh | `IBranchService` |
| Ca làm | `IShiftService` |
| Lịch ca | `IScheduleService` |
| Chấm công | `IAttendanceService` |
| Nhóm món / thực đơn | `ICategoryService`, `IMenuService` |
| Khu vực / bàn | `ITableManagementService` |
| Gọi món / bếp | `IOrderService` |
| Thanh toán | `IPaymentService`, `IBankTransferSettingsService` |
| Báo cáo | `IReportingService` |
| Audit | `IAuditService` |
| Cấu hình liên hệ | `ISupportContactSettingsService` |
| Người dùng hiện tại | `ICurrentUserService` |

Cách nói:

> “Interface service là API nội bộ của backend. Controller không gọi EF Core trực tiếp mà gọi interface; Infrastructure cung cấp implementation. Nhờ vậy có thể mock khi test và thay implementation mà không sửa controller.”

### 3.3. Endpoint HTTP chính

Route mặc định: `/{controller}/{action}/{id?}`.

| Nhóm | Endpoint tiêu biểu | Method | Trả về |
|------|--------------------|--------|--------|
| Đăng nhập | `/Account/Login` | GET, POST | HTML / redirect |
| Đăng xuất | `/Account/Logout` | POST | redirect |
| Đổi mật khẩu | `/Account/ChangePassword` | GET, POST | HTML / redirect |
| Trang chủ | `/` | GET | HTML |
| Chi nhánh | `/Branches`, `/Branches/Create`, `/Branches/Edit/{id}`, `/Branches/Delete/{id}` | GET/POST | HTML / redirect |
| Tài khoản | `/Accounts`, `/Accounts/Create`, `/Accounts/Edit/{id}`, `/Accounts/ResetPassword/{id}`, `/Accounts/ToggleActive` | GET/POST | HTML / redirect |
| Nhân viên | `/Employees`, `/Employees/Details/{id}`, `/Employees/Create`, `/Employees/Edit/{id}` | GET/POST | HTML / redirect |
| Ca làm | `/Shifts`, `/Shifts/Assignments`, `/Shifts/Assign/{id}` | GET/POST | HTML / redirect |
| Lịch ca | `/Schedules`, `/Schedules/Create`, `/Schedules/Delete` | GET/POST | HTML / redirect |
| Chấm công | `/Attendance`, `/Attendance/CheckIn`, `/Attendance/CheckOut`, `/Attendance/Approve` | GET/POST | HTML / redirect |
| Thực đơn | `/Menu`, `/Menu/Create`, `/Menu/Edit/{id}`, `/Menu/SetAvailable`, `/Menu/Restore` | GET/POST | HTML / redirect |
| Nhóm món | `/Categories`, `/Categories/Create`, `/Categories/Edit/{id}` | GET/POST | HTML / redirect |
| Bàn / khu vực | `/Tables`, `/Tables/Areas`, `/Tables/CreateArea`, `/Tables/EditArea/{id}` | GET/POST | HTML / redirect |
| Gọi món | `/Orders`, `/Orders/Open`, `/Orders/AddItem`, `/Orders/CancelItem`, `/Orders/MoveTable`, `/Orders/Merge` | GET/POST | HTML / redirect |
| Bếp | `/Kitchen/Queue`, `/Kitchen/SetStatus` | GET/POST | HTML / redirect |
| Thanh toán | `/Payments`, `/Payments/Checkout/{id}`, `/Payments/Pay`, `/Payments/Receipt/{id}` | GET/POST | HTML / redirect |
| QR chuyển khoản | `/Payments/TransferQr`, `/Payments/ReceiptTransferQr` | GET | ảnh PNG |
| Báo cáo | `/Reports`, `/Reports/Export` | GET | HTML / CSV |
| Audit | `/Audit` | GET | HTML |
| Cấu hình | `/BankTransferSettings`, `/SupportSettings` | GET/POST | HTML / redirect |
| Health check | `/health/db` | GET | JSON `{ "status": "healthy" }` |

### 3.4. Quy ước request/response

| Nội dung | Hiện tại |
|----------|----------|
| Kiểu request | Form HTML (`application/x-www-form-urlencoded`) |
| Kiểu response | HTML Razor, redirect 302; CSV; PNG; JSON cho health check |
| Xác thực | Cookie `RestaurantManagement.Auth`, hết hạn 8 giờ, sliding expiration |
| Phân quyền | Role + policy: `Management`, `Cashier`, `Kitchen`, `KitchenQueueView`, `PaymentManagement`, `OrderManagement` |
| CSRF | `[ValidateAntiForgeryToken]` trên các POST |
| Mã lỗi | Chủ yếu redirect kèm `TempData`; lỗi nghiệp vụ hiển thị trong trang; production có `/Home/Error` |
| JSON API | Chỉ có `/health/db`; không có CRUD JSON |
| Swagger/OpenAPI | Không có |
| JWT | Không có |

### 3.5. Vì sao không làm REST API/SPA?

Trả lời theo hướng phạm vi và kiến trúc:

> “Đồ án ưu tiên nghiệp vụ quản lý quán ăn và thời gian triển khai, nên chọn MVC server-rendered để giảm số tầng và độ phức tạp. Tầng Application đã tách interface service, nên nếu cần REST API chỉ cần thêm một project Web API gọi lại các interface này, không phải viết lại nghiệp vụ. Đổi sang SPA sẽ cần thêm API JSON, xác thực token, trạng thái phía client và kiểm thử E2E — nằm ngoài phạm vi hiện tại.”

Nếu bị hỏi “vậy API của em ở đâu?”:

> “API nội bộ nằm ở `Application/Abstractions`; API hướng ra ngoài là các action MVC. Em không có API JSON cho CRUD.”

---

## 4. Chỗ nào backend đã có nhưng frontend chưa có

Phần này nên nói rõ **3 mức** để tránh bị hiểu là “làm thiếu hết”:

1. Backend + dữ liệu đã sẵn sàng, **chỉ thiếu UI**.
2. Có schema/domain nhưng **chưa có service/API lẫn UI**.
3. **Chưa nằm trong phạm vi** (cả backend lẫn frontend).

### 4.1. Bảng tổng hợp

| # | Hạng mục | Backend hiện tại | Frontend hiện tại | Kết luận |
|---|----------|------------------|-------------------|----------|
| 1 | Chọn chi nhánh cho ADMIN | Có `BranchId`, `IBranchService.GetOptionsAsync`, query filter theo claim | Không có branch switcher; ADMIN có `BranchId = null` nên thấy toàn bộ dữ liệu | **Thiếu UI + thiếu tham số lọc chi nhánh cho ADMIN** |
| 2 | Báo cáo theo một chi nhánh | `ReportingService` lọc theo chi nhánh của phiên | Không có dropdown chọn chi nhánh; ADMIN chỉ xem tổng hợp | **Thiếu UI chọn chi nhánh** |
| 3 | Audit theo chi nhánh | `AuditLog` chưa có `BranchId`; `TableTransferLog` lọc được qua `Invoice` | Không có cột/filter chi nhánh | **Thiếu cả schema và UI** |
| 4 | Thực đơn/giá riêng từng chi nhánh | `MenuItem`, `Category` dùng chung toàn hệ thống | Không có màn hình giá theo chi nhánh | **Chưa có trong phạm vi** |
| 5 | Quản lý vị trí công việc (`Position`) | Có bảng + seed; dùng làm lookup cho nhân viên | Chỉ chọn trong form nhân viên, không có CRUD | **Thiếu màn hình CRUD** |
| 6 | Kỹ năng nhân viên (`Skill`, `EmployeeSkill`) | Có bảng trong schema/domain | Không có màn hình | **Chưa có service + UI** |
| 7 | Đồng phục (`UniformItem`, `UniformIssue`) | Có bảng trong schema/domain | Không có màn hình | **Chưa có service + UI** |
| 8 | Tham số hệ thống (`SystemParameter`) | Có bảng trong schema/domain | Không có màn hình | **Chưa có service + UI** |
| 9 | Phân quyền chi tiết (`Permission`, `RolePermission`) | Có bảng + seed; phân quyền đang hard-code theo role/policy | Không có ma trận phân quyền | **Thiếu UI + chưa dùng bảng permission trong runtime** |
| 10 | Thông báo realtime | Chưa có SignalR/WebSocket | Không có | **Chưa có trong phạm vi** |
| 11 | Đối soát thanh toán online | Có tạo QR VietQR và thu ngân xác nhận thủ công; chưa có webhook ngân hàng | Không có màn hình đối soát tự động | **Chưa có trong phạm vi** |
| 12 | Xuất báo cáo Excel/PDF | Chỉ có CSV | Nút xuất CSV | **Chưa có Excel/PDF** |
| 13 | REST API/SPA/mobile | Không có JSON CRUD API | Razor View | **Chưa có trong phạm vi** |

### 4.2. Cách trả lời từng nhóm

**Nhóm 1 — Chỉ thiếu UI (dữ liệu đã có):**

> “Ví dụ chọn chi nhánh cho ADMIN: dữ liệu `BranchId` và service lấy danh sách chi nhánh đã có, nhưng giao diện chưa có dropdown để chuyển ngữ cảnh. Hiện tại tài khoản ADMIN có `BranchId = null` nên query filter cho phép xem toàn bộ; muốn xem một chi nhánh cần bổ sung tham số lọc và truyền xuống service.”

**Nhóm 2 — Có schema nhưng chưa có service/UI:**

> “Các bảng như `Skill`, `UniformItem`, `UniformIssue`, `SystemParameter`, `Permission` đã có trong thiết kế CSDL và entity scaffold, nhưng chưa nằm trong luồng nghiệp vụ nên chưa có service, controller và màn hình. Đây là phần mở rộng, không phải chức năng đang hoạt động.”

**Nhóm 3 — Chưa có trong phạm vi:**

> “Thực đơn/giá theo chi nhánh, thông báo realtime, đối soát ngân hàng tự động và REST API chưa được triển khai. Nếu mở rộng, hướng làm là bổ sung bảng giá theo chi nhánh, SignalR, webhook và một project Web API dùng lại interface service.”

### 4.3. Phần frontend cố ý giới hạn, không phải thiếu

- **Tạo tài khoản:** không chọn chi nhánh riêng; chi nhánh lấy từ nhân viên được chọn.
- **Sửa tài khoản gắn nhân viên:** chi nhánh chỉ đọc; chỉ tài khoản không gắn nhân viên (ví dụ `bep`) mới được chọn chi nhánh.
- **ADMIN không thanh toán:** policy `PaymentManagement` chỉ cho MANAGER/CASHIER vì `Payment.PaidByEmployeeId` bắt buộc gắn nhân viên.
- **Xóa là xóa mềm:** UI không có “xóa vĩnh viễn”.
- **Báo cáo tự lọc theo chi nhánh của phiên:** người dùng thường không thấy dropdown chọn chi nhánh; đây là chủ ý phân tách dữ liệu.

### 4.4. Bảng “nếu giảng viên hỏi tại sao chưa làm”

| Câu hỏi | Trả lời gợi ý |
|---------|----------------|
| “Sao không có màn hình quản lý kỹ năng/đồng phục?” | “Các bảng này có trong thiết kế CSDL để đáp ứng yêu cầu mở rộng, nhưng chưa thuộc luồng nghiệp vụ bắt buộc của đồ án; em ưu tiên hoàn thiện luồng bán hàng, bếp, thanh toán, nhân sự và báo cáo.” |
| “Sao không phân quyền động?” | “Hiện phân quyền theo role + policy, đủ cho 6 vai trò của đồ án. Bảng `Permission`/`RolePermission` đã có trong schema để làm phân quyền động sau này.” |
| “Sao không có API JSON?” | “Đồ án dùng MVC server-rendered; em tách interface service ở Application nên có thể bọc thành Web API sau mà không đổi nghiệp vụ.” |
| “Sao ADMIN xem được tất cả chi nhánh?” | “Đúng, vì ADMIN là tài khoản hệ thống không gắn chi nhánh (`BranchId = null`); query filter chỉ áp dụng khi claim có chi nhánh. Muốn ADMIN xem riêng từng chi nhánh thì cần thêm bộ lọc.” |
| “Vì sao audit chưa lọc chi nhánh?” | “`AuditLog` hiện chưa có cột `BranchId`, nên chưa thể lọc chính xác; `TableTransferLog` thì lọc được qua hóa đơn. Đây là hạn chế đã ghi nhận.” |

---

## 5. Bộ câu hỏi — trả lời nhanh

### 5.1. Về kiến trúc

**Hỏi: “Vì sao chọn Clean Architecture?”**

> “Để tách nghiệp vụ khỏi hạ tầng: Domain không phụ thuộc EF Core, Application định nghĩa hợp đồng, Infrastructure cài đặt, Web chỉ giao tiếp. Cách này giúp thay DB/giao diện dễ hơn và dễ mock khi test.”

**Hỏi: “Business logic nằm ở đâu?”**

> “Nằm trong các service ở `Infrastructure`: `OrderService`, `PaymentService`, `TableManagementService`, `EmployeeService`… Controller chỉ điều phối. Ví dụ kiểm tra sức chứa bàn, trạng thái món, tính tiền, chặn gộp khác chi nhánh đều nằm trong service.”

**Hỏi: “Vì sao service implementation lại ở Infrastructure, không phải Application?”**

> “Vì implementation dùng EF Core và SQL Server — đó là chi tiết hạ tầng. Application chỉ giữ interface/DTO để không phụ thuộc EF. Đây là điểm có thể cải tiến nếu muốn tách use case thuần.”

**Hỏi: “Sao không dùng Repository/Unit of Work?”**

> “EF Core `DbContext` đã là Unit of Work, `DbSet` đã là repository. Thêm một lớp repository nữa sẽ tăng boilerplate mà chưa mang lại lợi ích rõ ràng ở quy mô đồ án.”

**Hỏi: “Vì sao dùng database-first, không dùng migration?”**

> “CSDL được quản lý bằng script SQL trong `database/` và entity được scaffold từ database để luôn khớp schema thật. Dự án chưa dùng EF Migrations; nếu chuyển sang code-first cần baseline migration đầu tiên.”

**Hỏi: “Vì sao dùng SQL View cho báo cáo?”**

> “Các view như `vw_RevenueByPeriod`, `vw_Payroll`, `vw_ItemSales` gom phép join/aggregate phức tạp ở database, giúp câu query báo cáo ngắn, nhất quán và tận dụng index. EF map view thành entity keyless để đọc.”

**Hỏi: “Branch scoping hoạt động thế nào?”**

> “Khi đăng nhập, `branch_id` được đưa vào claim. `CurrentUserService` đọc claim; `RestaurantDbContext` dùng `CurrentBranchId` trong 13 query filter toàn cục cho bàn, nhân viên, hóa đơn, chi tiết món, thanh toán, chấm công, lịch ca, lương… Các service báo cáo và order còn kiểm tra chéo chi nhánh khi chuyển/gộp bàn.”

**Hỏi: “Vì sao có 13 query filter mà vẫn phải kiểm tra trong service?”**

> “Query filter bảo vệ truy vấn đọc, nhưng không thay thế kiểm tra nghiệp vụ khi ghi. Ví dụ chuyển bàn phải so chi nhánh bàn đích với hóa đơn; gộp hóa đơn phải chặn hai chi nhánh khác nhau. Những kiểm tra đó nằm trong service và trả lỗi rõ ràng.”

### 5.2. Về API và giao diện

**Hỏi: “API của em là gì?”**

> “API hướng ra ngoài là các action MVC: GET trả Razor View, POST nhận form và redirect. API nội bộ là 16 interface service trong Application. Em không có REST API JSON, Swagger hay JWT.”

**Hỏi: “Sao không thấy Swagger?”**

> “Vì không dùng `[ApiController]`/Web API. Toàn bộ endpoint nằm trong controller MVC. Chỉ có `/health/db` trả JSON để kiểm tra kết nối database.”

**Hỏi: “Frontend gọi backend bằng gì?”**

> “Form HTML submit trực tiếp, kèm antiforgery token; server render lại Razor View hoặc redirect. Không có `fetch`/AJAX gọi API JSON cho CRUD.”

**Hỏi: “Frontend nằm ở đâu?”**

> “Trong `RestaurantManagement.Web/Views`, dùng Razor, Bootstrap 5, jQuery validation. Không có React/Vue/Angular.”

**Hỏi: “Phần nào chưa có frontend?”**

> “Các phần chính đã có màn hình. Phần chưa có UI gồm: chọn chi nhánh cho ADMIN, lọc báo cáo theo chi nhánh, audit theo chi nhánh; các bảng mở rộng như kỹ năng, đồng phục, tham số hệ thống, phân quyền động chưa có màn hình; thực đơn/giá theo chi nhánh và REST API chưa thuộc phạm vi.”

### 5.3. Về bảo mật

**Hỏi: “Xác thực và phân quyền thế nào?”**

> “Cookie authentication; mật khẩu hash bằng `PasswordHasher<AppUser>` (PBKDF2-HMAC-SHA512). Mỗi request kiểm tra lại trạng thái tài khoản, nhân viên, chi nhánh và security stamp; tài khoản bị khóa/đổi quyền/đổi mật khẩu sẽ bị đăng xuất. Phân quyền theo role và policy trong `Program.cs`.”

**Hỏi: “Chống CSRF thế nào?”**

> “Các POST đều có `[ValidateAntiForgeryToken]`; form Razor sinh token tự động. Cookie `HttpOnly`, `SameSite=Lax`.”

**Hỏi: “Chống SQL injection thế nào?”**

> “Truy vấn dùng EF Core với tham số hóa; không có raw SQL trong luồng ứng dụng. Script SQL chỉ dùng để khởi tạo/migrate database.”

**Hỏi: “Audit log ghi gì?”**

> “Trigger SQL Server ghi INSERT/UPDATE/DELETE vào `AuditLog`, lấy người thao tác từ `SESSION_CONTEXT`; mật khẩu không bị ghi. Trang `/Audit` cho phép lọc ngày, hành động, từ khóa và xem lịch sử chuyển/gộp bàn.”

### 5.4. Về dữ liệu và transaction

**Hỏi: “Transaction dùng ở đâu?”**

> “Mở bàn, chuyển bàn, gộp hóa đơn và thanh toán dùng transaction `Serializable` để tránh hai người thao tác cùng bàn/hóa đơn. Một số thao tác thêm món dùng `ReadCommitted`.”

**Hỏi: “Xử lý tranh chấp bàn thế nào?”**

> “Kết hợp transaction `Serializable`, kiểm tra bàn đang có hóa đơn mở và ràng buộc unique trên số bàn theo chi nhánh. Nếu hai request cùng mở một bàn, một request sẽ thất bại với thông báo nghiệp vụ.”

**Hỏi: “Vì sao lưu UTC?”**

> “Database lưu UTC (`SYSUTCDATETIME`, `DateTime.UtcNow`) để tránh lệch múi giờ; giao diện chuyển sang giờ Việt Nam khi hiển thị. `Schedule.WorkDate` dùng ngày địa phương vì là ngày làm việc.”

**Hỏi: “Số bàn có được trùng giữa các chi nhánh không?”**

> “Có. Ràng buộc là unique theo `(BranchId, TableNumber)`, nên `B01` ở chi nhánh 1 và `B01` ở chi nhánh 2 là hợp lệ; trong cùng chi nhánh thì không được trùng.”

### 5.5. Chính sách ADMIN đã chốt (2026-11-13)

Định hướng được chốt: **ADMIN quản trị xuyên chi nhánh, nhưng giữ nguyên giá trị cốt lõi là tài khoản quản trị hệ thống — không thay thế vai trò vận hành tại chi nhánh.**

| Nội dung | Quyết định |
|----------|-----------|
| Phạm vi dữ liệu | ADMIN xem và quản trị dữ liệu của tất cả chi nhánh |
| Quản lý chi nhánh | Chỉ ADMIN được tạo/sửa/xóa mềm chi nhánh |
| Quản lý tài khoản | Chỉ ADMIN; tài khoản gắn nhân viên lấy chi nhánh từ nhân viên |
| Nhân viên, bàn, thực đơn, báo cáo, audit | ADMIN được quản lý/xem xuyên chi nhánh |
| Mở bàn, gọi món, thanh toán, chấm công | **Không** cấp cho ADMIN; tiếp tục dùng MANAGER/CASHIER/WAITER theo chi nhánh |
| Cách xác định phạm vi | ADMIN có `BranchId = NULL`; query filter bỏ điều kiện chi nhánh |
| Nếu cần vai trò toàn quyền kinh doanh | Tạo tài khoản `OWNER` riêng, không mở rộng ADMIN |
| Phần còn thiếu | Chưa có branch switcher/bộ lọc chi nhánh cho ADMIN; nếu bổ sung chỉ là tiện ích lọc, không thay đổi quyền |

Câu trả lời khi bị hỏi:

> “ADMIN là tài khoản quản trị hệ thống nên được quản trị xuyên chi nhánh: cấu hình, tài khoản, nhân viên, bàn, báo cáo và audit. Nhưng ADMIN không thanh toán, không mở bàn và không chấm công, vì các nghiệp vụ đó phải gắn với hồ sơ nhân viên và cần tách biệt trách nhiệm. Nếu cần toàn quyền kinh doanh, em dùng role OWNER riêng.”

### 5.6. Về kiểm thử và chất lượng

**Hỏi: “Đã test gì?”**

> “Có 11 kiểm thử tích hợp nghiệp vụ trong `BusinessWorkflowTests`: chuyển bàn, gộp hóa đơn, khách theo bàn, biên nhận, duyệt công, ca chồng giờ, tuần ISO và phân tách chi nhánh. Build hiện tại 0 warning/0 error. Trong môi trường demo không có SQL Server test nên 11 test được skip, không failed.”

**Hỏi: “Vì sao test bị skip?”**

> “Test cần biến `RESTAURANT_TEST_CONNECTION_STRING` trỏ tới SQL Server riêng. Môi trường hiện tại không bật dịch vụ đó nên test tự skip để không ảnh hưởng database demo.”

**Hỏi: “Điểm yếu lớn nhất của đồ án?”**

> “Ba điểm: một là business logic đặt trong Infrastructure nên chưa phải Clean Architecture thuần; hai là chưa có REST API/SPA; ba là một số bảng mở rộng và phân tách chi nhánh ở mức báo cáo/audit chưa hoàn thiện. Em đã ghi rõ trong tài liệu triển khai để không nói quá phạm vi.”

---

## 6. Kịch bản demo 5 phút

1. **Đăng nhập `manager`** → giới thiệu layout, chi nhánh hiển thị trên thanh tài khoản.
2. **Bàn ăn** → mở bàn, gọi món; chỉ ra bàn thuộc chi nhánh và menu dùng chung.
3. **Bếp** → đăng nhập `bep`, nhận món và hoàn thành.
4. **Thanh toán** → đăng nhập `thungan`, checkout, xem biên nhận.
5. **Báo cáo** → xem doanh thu/lương, nói báo cáo tự lọc theo chi nhánh của phiên.
6. **Quản trị** → đăng nhập `admin`, xem danh sách chi nhánh, tài khoản, nhân viên; nhấn mạnh ADMIN thấy toàn bộ.
7. **Kết luận kỹ thuật** → chỉ vào `Program.cs`, `RestaurantDbContext`, một service và một Razor View để chứng minh luồng MVC + Clean Architecture.

---

## 7. File nên mở khi bị hỏi

| Nếu hỏi về | Mở file |
|------------|---------|
| Kiến trúc tổng thể | `src/README.md`, `src/RestaurantManagement.slnx` |
| Composition root/DI | `Web/Program.cs`, `Infrastructure/DependencyInjection.cs` |
| Entity/DbContext/query filter | `Infrastructure/Persistence/RestaurantDbContext.cs` |
| Nghiệp vụ đơn hàng | `Infrastructure/Orders/OrderService.cs` |
| Nghiệp vụ thanh toán | `Infrastructure/Payments/PaymentService.cs` |
| Phân tách chi nhánh | `Branch.cs`, `CurrentUserService.cs`, `BranchService.cs` |
| Báo cáo | `database/03_views.sql`, `ReportingService.cs` |
| Xác thực/phân quyền | `UserAuthenticationService.cs`, `Program.cs` |
| API nội bộ | `Application/Abstractions/*.cs` |
| Giao diện | `Web/Views/**/*.cshtml` |
| Kiểm thử | `Tests/BusinessWorkflowTests.cs` |
| Nhật ký sửa đổi | `docs/28_ImplementationLog_2026-11-13_BranchManagement.md` |

---

## 8. Số liệu nên nhớ

| Hạng mục | Giá trị |
|----------|---------|
| Target framework | .NET 10 |
| Số project | 5 (`Domain`, `Application`, `Infrastructure`, `Web`, `Tests`) |
| Số bảng | 26 |
| Số view | 10 |
| Số entity scaffold | 36 |
| Số interface service nghiệp vụ | 16 (+ `ICurrentUserService`) |
| Số query filter theo chi nhánh | 13 |
| Số controller | 18 |
| Số kiểm thử tích hợp | 11 |
| Build hiện tại | 0 warning, 0 error |
| Cổng chạy mặc định | `http://localhost:5010` |
| Tài khoản demo | `admin`, `manager`, `sale`, `thungan`, `bep` — mật khẩu `Admin@123` |

---

## 9. Ba câu không nên nói

1. **“Em có REST API/Swagger.”** → Sai; chỉ có MVC endpoint và `/health/db`.
2. **“Clean Architecture hoàn chỉnh, business logic nằm ở Application.”** → Không đúng; implementation service đang ở Infrastructure.
3. **“Tất cả chức năng đều đã có frontend.”** → Không đúng; còn các mục ở mục 4, đặc biệt là chọn/lọc chi nhánh và các bảng mở rộng.

Cách nói an toàn:

> “Phần nghiệp vụ chính đã có đủ backend và giao diện. Các phần còn lại em chia thành ba nhóm: thiếu UI nhưng đã có dữ liệu, có schema nhưng chưa có service/UI, và chưa nằm trong phạm vi. Em ghi rõ trong tài liệu để tránh hiểu nhầm là đã hoàn thiện toàn bộ.”
