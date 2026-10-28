# Development Roadmap

| Phase | Nội dung | Trạng thái |
|-------|----------|-----------|
| Phase 1 | Project Initialization | ✅ 2026-10-27 |
| Phase 2 | Database Design | ✅ 2026-10-26 |
| Phase 3 | Authentication | ✅ 2026-10-27 |
| Phase 4 | Employee Management | ✅ 2026-10-27 (hồ sơ + ca làm + chấm công + tài khoản) |
| Phase 5 | Menu Management | ✅ 2026-10-28 |
| Phase 6 | Order Processing | ✅ 2026-10-28 |
| Phase 7 | Payment | ✅ 2026-10-28 |
| Phase 8 | Reporting | ✅ 2026-10-28 |

## Ghi chú tiến độ

| Phase | Sản phẩm bàn giao | Tài liệu |
|-------|-------------------|----------|
| 1 | Solution 4 project (Clean Architecture), DbContext + 35 entity scaffold, DI, `/health/db` | `13_ImplementationLog_2026-10-27.md` |
| 2 | 25 bảng, 10 view, 7 trigger, Docker SQL Server 2022 | `11_DatabaseSchemaDesign.md`, `12_ImplementationLog.md` |
| 3 | Đăng nhập/đăng xuất/đổi mật khẩu, cookie auth, policy theo vai trò, `ICurrentUserService` | `14_ImplementationLog_2026-10-27_Phase3.md` |
| 4a | CRUD hồ sơ nhân viên, tìm kiếm/lọc/phân trang, xóa mềm, quy tắc nghiệp vụ | `15_ImplementationLog_2026-10-27_Phase4.md` |
| 4b | Ca làm, phân ca nhân viên, xếp ca theo ngày, chấm công cơ bản (check-in/out, duyệt công) | `16_ImplementationLog_2026-10-27_Phase4b.md` |
| 4c | Quản lý tài khoản: cấp tài khoản cho nhân viên, đổi vai trò, đặt lại mật khẩu, khóa/mở; kiểm tra phiên mỗi request | `17_ImplementationLog_2026-10-27_Phase4c.md` |
| 5 | Quản lý nhóm món, thực đơn, giá/tình trạng món, khu vực và sơ đồ bàn theo trạng thái | `18_ImplementationLog_2026-10-28_Phase5.md` |
| 6 | Mở bàn/hóa đơn, gọi/thêm/đổi/hủy món, hàng đợi bếp FIFO, chuyển/gộp bàn và hóa đơn | `19_ImplementationLog_2026-10-28_Phase6.md` |
| 7 | Danh sách hóa đơn chờ thanh toán, xác nhận món chưa xong, giảm giá theo quyền, tiền mặt/chuyển khoản, đóng hóa đơn, giải phóng bàn và in hóa đơn | `20_ImplementationLog_2026-10-28_Phase7.md` |
| 8 | Báo cáo doanh thu, khách theo ca/nhân viên/bàn, món bán, ca đã duyệt/lương; bộ lọc, in và xuất CSV | `21_ImplementationLog_2026-10-28_Phase8.md` |
| Audit | Nhật ký thao tác có người thực hiện, lọc/phân trang, lịch sử chuyển/gộp bàn | `22_ImplementationLog_2026-10-28_Audit.md` |

> Thứ tự thực hiện thực tế: Phase 2 (CSDL) làm trước Phase 1 (khởi tạo solution) để scaffold database-first.
> Phase 5–8 theo thứ tự trong bảng, mỗi phase cập nhật trạng thái và tạo nhật ký riêng.
