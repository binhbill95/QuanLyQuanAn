# Role — Vai trò người dùng

Hệ thống dùng **6 vai trò** (khớp bảng `Role` trong CSDL):

| # | Code | Tên hiển thị | Phạm vi |
|---|------|--------------|---------|
| 1 | `ADMIN` | Admin | Quản trị hệ thống: tài khoản, phân quyền, cấu hình, xem toàn bộ dữ liệu. Không gắn với hồ sơ nhân viên. |
| 2 | `OWNER` | Chủ quán | Xem báo cáo, doanh thu, cấu hình giá/khuyến mãi. |
| 3 | `MANAGER` | Quản lý | Nhân viên, ca làm, chấm công, bàn, thực đơn, báo cáo. |
| 4 | `CASHIER` | Thu ngân | Mở/đóng bàn, thanh toán, in hóa đơn. |
| 5 | `WAITER` | Phục vụ | Chọn bàn, gọi món, chuyển món ra bàn. |
| 6 | `KITCHEN` | Bếp | Nhận món, cập nhật trạng thái chế biến. |

## Vì sao 6 vai trò?

- Đề bài (PDF) mô tả **4 nhóm người dùng**: quản lý, phục vụ, bếp, thu ngân.
- Bản tài liệu trước ghi **5 vai trò** (Admin, Manager, Cashier, Waiter, Kitchen).
- CSDL và seed hiện tại dùng **6 vai trò**: tách `ADMIN` (quản trị kỹ thuật) khỏi `OWNER` (chủ quán) vì hai người này có quyền khác nhau.
- Chỉ `MANAGER`/`CASHIER` thanh toán vì `Payment.PaidByEmployeeId` bắt buộc gắn nhân viên; `ADMIN` không có hồ sơ nhân viên để lưu người thu tiền.

→ Đã thống nhất dùng **6 vai trò**; `docs/11_DatabaseSchemaDesign.md` và `database/02_seed.sql` là nguồn chuẩn.

## Ma trận quyền (tóm tắt)

| Chức năng | ADMIN | OWNER | MANAGER | CASHIER | WAITER | KITCHEN |
|-----------|:-----:|:-----:|:-------:|:-------:|:------:|:-------:|
| Quản lý tài khoản / phân quyền | ✅ | — | — | — | — | — |
| Cấu hình hệ thống | ✅ | ✅ | — | — | — | — |
| Quản lý nhân viên, ca, chấm công | ✅ | — | ✅ | — | — | — |
| Quản lý thực đơn | ✅ | ✅ | ✅ | — | — | — |
| Mở bàn / gọi món | ✅ | — | ✅ | ✅ | ✅ | — |
| Thanh toán | — | — | ✅ | ✅ | — | — |
| Cập nhật trạng thái món (bếp) | ✅ | — | ✅ | — | — | ✅ |
| Xem báo cáo doanh thu | ✅ | ✅ | ✅ | — | — | — |

> Quyền chi tiết ở mức từng thao tác được kiểm soát bằng `RolePermission` (bảng có sẵn trong CSDL) — sẽ dùng ở các phase sau.
> Ứng dụng dùng **policy** `Management` (ADMIN, OWNER, MANAGER), `Cashier`, `Kitchen` và `PaymentManagement` (MANAGER, CASHIER); ADMIN không thanh toán vì không có hồ sơ nhân viên để lưu người thu tiền.
> Màn hình quản lý tài khoản `/Accounts` đã hiện thực ở Phase 4c — chỉ `ADMIN` truy cập (xem `17_ImplementationLog_2026-10-27_Phase4c.md`).
