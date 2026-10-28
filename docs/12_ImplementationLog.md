# Implementation Log — Phiên 2026-10-26

> Nhật ký thực hiện để lưu lại quá trình, quyết định và kết quả kiểm chứng.
> Mục đích: người đọc lại (hoặc phiên AI sau) hiểu **đã làm gì, vì sao, kiểm chứng ra sao, còn gì chưa làm**.
> **Phiên tiếp theo:** `13_ImplementationLog_2026-10-27.md` (Phase 1 — khởi tạo solution + scaffold EF Core) → `14_ImplementationLog_2026-10-27_Phase3.md` (Phase 3 — xác thực & phân quyền) → `15_ImplementationLog_2026-10-27_Phase4.md` (Phase 4 — quản lý nhân viên).

## 1. Thông tin phiên

| Hạng mục | Nội dung |
|----------|----------|
| Ngày | 2026-10-26 |
| Dự án | Restaurant Management System (`G:\Project_Quanlyquanan\RestaurantManagement`) |
| Phạm vi | Hoàn thiện bộ tài liệu 01–09 → thiết kế CSDL → sinh script SQL → dựng Docker → chạy thử |
| Kết quả ban đầu | 25 bảng, 10 view, 7 trigger, 33 FK — chạy thành công trên SQL Server 2022 container; audit hiện được mở rộng lên 10 trigger trong `22_ImplementationLog_2026-10-28_Audit.md` |
| Công cụ | Docker 29.8.0, Docker Compose v5.5.1, sqlcmd (mssql-tools18) |

## 2. Đầu vào đã đọc

| Tệp | Nội dung sử dụng |
|-----|------------------|
| `10_Quan-ly-quan-an.pdf` | Đề bài gốc: 2 quy trình nghiệp vụ, 4 actor, danh sách chức năng, yêu cầu nộp bài, bảng thiết kế lớp (bỏ trống) |
| `01_ProjectOverview.md` | Mục tiêu, công nghệ (ASP.NET Core MVC, EF Core, SQL Server 2022 Docker) |
| `02_BusinessRequirements.md` | 16 business rule, NFR, điểm mâu thuẫn của đề |
| `03_UserRoles.md` | 5 vai trò ban đầu (Admin, Manager, Cashier, Waiter, Kitchen) |
| `04_UseCases.md` | 34 use case, ma trận UC × Actor |
| `05_SystemModules.md` | 13 module, chấm công giữ mức cơ bản |
| `06_DatabaseRequirements.md` | 21 bảng đề xuất ban đầu |
| `07–09` | Tech stack, coding rules (Clean Architecture/DI/Repository/Service/DTO), roadmap 8 phase |

## 3. Dòng thời gian thực hiện

| # | Bước | Kết quả |
|---|------|---------|
| 1 | Trích xuất nội dung PDF bằng PyMuPDF (model không đọc trực tiếp PDF) | 3 trang, đủ nội dung; xác định bảng thiết kế lớp trong đề bị bỏ trống |
| 2 | Viết lại `02_BusinessRequirements.md` | 2 quy trình + sơ đồ, 16 BR, NFR, 8 điểm mâu thuẫn, yêu cầu nộp bài |
| 3 | Viết lại `04_UseCases.md` | 34 UC (B/T), chi tiết 15 UC trọng tâm, ma trận quyền |
| 4 | Viết lại `05_SystemModules.md` | 13 module, mỗi module có bảng chức năng + actor + ưu tiên |
| 5 | Viết lại `06_DatabaseRequirements.md` | 21 bảng, kiểu dữ liệu, truy vết báo cáo, bảng thiết kế lớp mẫu |
| 6 | Tinh giản chấm công | Module 12 còn 4 chức năng; bỏ `Payroll`, dùng `Employee.ShiftRate` |
| 7 | Thiết kế CSDL hoàn chỉnh → `11_DatabaseSchemaDesign.md` | 2 ERD Mermaid, 25 bảng, 32 quan hệ, đặc tả cột, chứng minh 3NF, 10 view, phương án Docker |
| 8 | Sinh script SQL | `01_schema.sql`, `02_seed.sql`, `03_views.sql`, `04_audit.sql`, `05_rules.sql` |
| 9 | Dựng Docker | `Dockerfile`, `entrypoint.sh`, `docker-compose.yml`, `.env.example` |
| 10 | Chạy container lần 1 | Init thành công 4 script; phát hiện 3 lỗi (mục 6) |
| 11 | Sửa lỗi + rebuild từ volume trắng | 5 script chạy sạch |
| 12 | Chạy bộ kiểm thử nghiệp vụ | 10 nhóm kiểm thử, tất cả đạt (mục 5) |
| 13 | Viết README cho `database/`, `docker/` và nhật ký này | Hoàn tất |

## 4. Quyết định thiết kế & lý do

| # | Quyết định | Lý do |
|---|-----------|-------|
| 1 | Đổi `User` → **`AppUser`** | `USER` là từ khóa hệ thống của SQL Server |
| 2 | Seed **6 vai trò** (ADMIN, OWNER, MANAGER, CASHIER, WAITER, KITCHEN) | Dung hòa `03_UserRoles.md` (5 role) và đề PDF (4 actor: Chủ quán, Quản lý, Phục vụ, Bếp) |
| 3 | Ca làm việc là **bảng cấu hình**, seed 2 ca 09–16 / 16–22 | Đề mâu thuẫn 3 ca vs 2 ca; không hard-code |
| 4 | `Attendance` chỉ giữ `ScheduleId` (1-1) | Loại bỏ phụ thuộc bắc cầu `EmployeeId/ShiftId/WorkDate` → 3NF |
| 5 | Không lưu tổng tiền hóa đơn, không lưu trạng thái bàn | Dữ liệu suy diễn → tính bằng view, tránh lệch |
| 6 | `InvoiceDetail.UnitPrice` lưu giá tại thời điểm gọi | Bảo toàn hóa đơn lịch sử khi đổi giá món |
| 7 | `Invoice.ShiftId` lưu ca tại thời điểm giao dịch | Cấu hình ca có thể thay đổi về sau |
| 8 | `InvoiceTable` có `ReleasedAt` + unique filtered index | Hỗ trợ ghép bàn/chuyển bàn, vẫn báo cáo đúng theo bàn |
| 9 | Toàn bộ FK `NO ACTION`, soft delete | Bảo toàn dữ liệu báo cáo, tránh xóa dây chuyền |
| 10 | Audit 7 cột trên mọi bảng + `AuditLog` + 10 trigger audit | Truy vết thay đổi dữ liệu quan trọng |
| 11 | Thêm `05_rules.sql` (2 trigger) | Phát hiện trong test: CSDL cho phép hủy món đã hoàn thành → thêm lớp bảo vệ |
| 12 | Collation đặt ở mức **database** (`Vietnamese_CI_AS`) | `MSSQL_COLLATION` không phải biến chính thức của image; đặt trong script chắc chắn hơn |
| 13 | `datetime2(0)` + lưu UTC | Đủ chính xác cho nghiệp vụ, tránh lệch múi giờ |

## 5. Kết quả kiểm chứng

### 5.1 Cấu trúc

| Hạng mục | Kỳ vọng | Thực tế |
|----------|:-------:|:-------:|
| Bảng | 25 | **25** ✔ |
| View | 10 | **10** ✔ |
| Trigger | 7 (5 audit + 2 rule) | **7** ✔ |
| Khóa ngoại | 33 | **33** ✔ |

### 5.2 Dữ liệu seed

| Bảng | Số dòng |
|------|:-------:|
| Role | 6 |
| Position | 4 |
| Employee | 4 |
| AppUser | 2 |
| Shift | 2 |
| DiningTable | 10 |
| Category | 4 |
| MenuItem | 14 |
| Schedule | 6 |
| SystemParameter | 6 |

### 5.3 Ca kiểm thử nghiệp vụ

| # | Ca kiểm thử | Kỳ vọng | Kết quả |
|---|-------------|---------|---------|
| T1 | Mở bàn B01 cho 4 khách, gọi 3 món | Trạng thái bàn = `Đang đợi món` | ✔ |
| T2 | Xem `vw_KitchenQueue` | 2 món chưa xong, `WaitingMinutes >= 0` | ✔ (0) |
| T3 | `vw_InvoiceSummary` | SubTotal = 245.000 | ✔ |
| T4 | Hủy món **đã hoàn thành** | Bị chặn | ✔ (trigger R1) |
| T5 | Hủy món **đang chờ** | Được phép, không tính tiền | ✔ (Status = 3, SubTotal = 0) |
| T6 | Bếp hoàn thành tất cả món | Trạng thái bàn = `Món đã xong` | ✔ |
| T7 | Thanh toán + giải phóng bàn | Bàn = `Trống` | ✔ |
| T8 | Thêm món vào hóa đơn **đã thanh toán** | Bị chặn | ✔ (trigger R2) |
| T9 | Báo cáo doanh thu / ca / món / nhân viên | Khớp 245.000 và 4 khách | ✔ |
| T10 | `AuditLog` ghi INSERT/UPDATE | Có bản ghi cho Invoice, InvoiceDetail, Payment | ✔ |
| T11 | Dọn dẹp dữ liệu test | Về 0, không ảnh hưởng seed | ✔ |

### 5.4 Lệnh kiểm chứng

```bash
# Trạng thái container (kỳ vọng: healthy)
docker compose ps

# Kiểm tra nhanh
docker exec -it restaurant-sqlserver bash -lc \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
   -Q "SELECT name FROM sys.tables ORDER BY name"'
```

Bộ test đầy đủ nằm ở `%TEMP%\opencode\verify2.sql` (không commit vào repo vì là script kiểm thử tạm).

### 5.5 Kiểm tra tính bền vững dữ liệu

| Ca kiểm thử | Kỳ vọng | Kết quả |
|-------------|---------|---------|
| Recreate container (giữ volume) | Bỏ qua init, dữ liệu còn nguyên | ✔ log: "Database đã tồn tại -> bỏ qua init"; 4 Employees / 14 MenuItems / 10 Tables |
| Healthcheck | `healthy` | ✔ |
| `docker compose down -v` + `up --build` | Init lại từ đầu, 5 script chạy sạch | ✔ |

## 6. Lỗi phát hiện & cách xử lý

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|-----|-------------|-----------|
| 1 | `02_seed.sql` có `BEGIN TRANSACTION` nhưng thiếu `COMMIT` sau khi sửa | Sửa file thủ công làm mất cặp lệnh | Bỏ transaction bao ngoài; seed idempotent bằng `IF NOT EXISTS` |
| 2 | `EmployeeSkill` gán sai kỹ năng | `OR ... AND NOT EXISTS` sai thứ tự ưu tiên toán tử | Bọc nhóm `OR` trong ngoặc đơn |
| 3 | CSDL cho phép hủy món đã hoàn thành | CHECK constraint chỉ kiểm tra metadata, không chặn chuyển trạng thái | Thêm `05_rules.sql` với trigger `trg_InvoiceDetail_CancelGuard` (R1) |
| 4 | `WaitingMinutes = -1` | `datetime2(0)` làm tròn giây + `DATEDIFF(MINUTE)` vắt qua mốc phút | Tính theo `SECOND/60` và chặn giá trị âm |
| 5 | Test T5 báo `LOI` sai | Script test đọc cột `Status` từ `vw_InvoiceSummary` (status của **hóa đơn**, không phải dòng món) | Kiểm tra trực tiếp `InvoiceDetail.Status`; xác nhận lại Status = 3, SubTotal = 0 |
| 6 | Rủi ro mất dấu tiếng Việt khi nạp script | sqlcmd đọc file UTF-8 | Thêm `-f 65001` vào `entrypoint.sh`; đã xác nhận dữ liệu có dấu đúng |
| 7 | Healthcheck báo `unhealthy` dù SQL Server chạy | Thiếu `-C` (trust certificate) — mssql-tools18 mặc định mã hóa và từ chối cert tự ký | Thêm `-C` vào healthcheck trong `docker-compose.yml`; kiểm tra lại → `healthy` |

## 7. Tệp đã tạo / cập nhật

| Tệp | Trạng thái | Ghi chú |
|-----|-----------|---------|
| `docs/02_BusinessRequirements.md` | Viết lại | Phân tích đề + BR + NFR + mâu thuẫn |
| `docs/04_UseCases.md` | Viết lại | 34 UC |
| `docs/05_SystemModules.md` | Viết lại | 13 module |
| `docs/06_DatabaseRequirements.md` | Viết lại | 21 bảng yêu cầu |
| `docs/11_DatabaseSchemaDesign.md` | Mới | Thiết kế CSDL hoàn chỉnh |
| `docs/12_ImplementationLog.md` | Mới | Nhật ký này |
| `database/01_schema.sql` | Mới | DDL 25 bảng |
| `database/02_seed.sql` | Mới | Seed idempotent |
| `database/03_views.sql` | Mới | 10 view |
| `database/04_audit.sql` | Mới | 5 trigger audit |
| `database/05_rules.sql` | Mới | 2 trigger nghiệp vụ |
| `database/README.md` | Mới | Hướng dẫn thứ tự chạy |
| `database/backup/.gitkeep` | Mới | Giữ thư mục backup |
| `docker/Dockerfile` | Mới | Image + entrypoint |
| `docker/entrypoint.sh` | Mới | Tự init một lần |
| `docker/docker-compose.yml` | Mới | Service + volume + healthcheck |
| `docker/.env.example`, `.env` | Mới | Biến môi trường (`.env` không commit) |
| `docker/README.md` | Mới | Cách chạy / backup / reset |

## 8. Cách chạy lại từ đầu

```bash
cd G:\Project_Quanlyquanan\RestaurantManagement\docker
docker compose down -v
docker compose up -d --build
docker compose logs -f sqlserver     # chờ "Khởi tạo database hoàn tất"
```

Kết nối: `Server=localhost,1433;Database=RestaurantManagement;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True`

## 9. Việc còn lại (TODO)

| # | Việc | Ưu tiên |
|---|------|:-------:|
| 1 | Khởi tạo solution ASP.NET Core 9 MVC theo Clean Architecture (Phase 1) | Cao |
| 2 | Sinh entity/DbContext từ schema (EF Core scaffold hoặc code-first đồng bộ) | Cao |
| 3 | Chọn phương án Identity (A: `ApplicationUser : IdentityUser<int>`) và map `AppUser` | Cao |
| 4 | Thêm script backup định kỳ (SQL Agent job hoặc cron host) | Trung bình |
| 5 | Bổ sung test cho `vw_Payroll` (cần dữ liệu chấm công đã duyệt) | Trung bình |
| 6 | Seed thêm hóa đơn mẫu nhiều tháng để demo báo cáo quý/năm | Thấp |
| 7 | Tạo login riêng cho ứng dụng (bỏ dùng `sa`) | Trung bình |
| 8 | Chốt lại 5 hay 6 vai trò với giảng viên | Trung bình |

## 10. Ghi chú khi cập nhật tài liệu

1. **Thay đổi schema** → sửa `docs/11` trước, sau đó cập nhật `database/01_schema.sql` (và migration EF Core), cuối cùng ghi một dòng vào mục 3 của file này.
2. **Thay đổi quy tắc nghiệp vụ** → cập nhật `docs/02` (BR), `docs/04` (UC), rồi mới đến trigger `05_rules.sql`.
3. **Không sửa `database/01_schema.sql` sau khi đã phát sinh dữ liệu thật** — tạo script migration mới.
4. Mọi kết quả kiểm chứng phải ghi rõ **kỳ vọng vs thực tế**, kể cả khi đạt.
