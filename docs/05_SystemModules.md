# System Modules

> Giữ nguyên 10 module đã chốt, bổ sung 3 module còn thiếu so với đề bài (Ca & lịch làm việc, Chấm công & lương, Cấu hình hệ thống).
> Ưu tiên: **B** bắt buộc — **T** tùy chọn.

## 1. Tổng quan module

| Module | Tên | Ưu tiên | Use Case |
|--------|-----|---------|----------|
| Module 1 | Authentication | B | UC001–UC003 |
| Module 2 | Employee Management | B | UC014, UC015, UC023 |
| Module 3 | Role Management | B (chi tiết: T) | UC033 |
| Module 4 | Menu Management | B | UC025, UC026 |
| Module 5 | Category Management | B | UC025 |
| Module 6 | Table Management | B | UC004, UC024 |
| Module 7 | Order Management | B | UC005–UC010 |
| Module 8 | Kitchen Management | B | UC012, UC013 |
| Module 9 | Payment Management | B | UC011 |
| Module 10 | Reporting | B | UC027–UC032 |
| Module 11 | Shift & Schedule Management | B | UC016–UC019 |
| Module 12 | Attendance & Payroll | B | UC020–UC022 |
| Module 13 | System Configuration | T | UC034 |

## 2. Chi tiết chức năng từng module

### Module 1 — Authentication
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Đăng nhập bằng tài khoản nội bộ | Tất cả | B |
| Đăng xuất | Tất cả | B |
| Đổi mật khẩu | Tất cả | B |
| Điều hướng giao diện theo quyền | Tất cả | B |
| Khóa tài khoản khi nhân viên nghỉ việc | Chủ quán | B |

### Module 2 — Employee Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Thêm nhân viên (hồ sơ đầy đủ) | Chủ quán | B |
| Sửa hồ sơ nhân viên | Chủ quán | B |
| Xóa/ngừng dùng nhân viên | Chủ quán | B |
| Xem danh sách, tìm kiếm, lọc theo vị trí/trạng thái | Chủ quán | B |
| Gán trình độ vi tính (Word, Excel, Cài phần mềm, Khác) | Chủ quán | B |
| Gán ca có thể làm việc | Chủ quán | B |
| Ghi nhận dụng cụ/trang phục đã nhận & đã trả | Chủ quán | B |
| Cho nhân viên nghỉ việc (đổi trạng thái, xóa lịch, khóa tài khoản) | Chủ quán | B |

### Module 3 — Role Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Gán vai trò cho tài khoản (Chủ quán, Quản lý, Phục vụ, Bếp) | Chủ quán | B |
| Tạo/sửa/xóa loại người dùng | Chủ quán | T |
| Thêm/xóa chức năng cho mỗi loại người dùng | Chủ quán | T |

### Module 4 — Menu Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Thêm/sửa/xóa món ăn, nước uống | Chủ quán | B |
| Cập nhật giá bán, đơn vị tính, hình ảnh | Chủ quán | B |
| Đánh dấu món hết / còn | Chủ quán, Bếp | B |
| Xem thực đơn theo nhóm món | Tất cả | B |

### Module 5 — Category Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Thêm/sửa/xóa nhóm món (khai vị, món chính, tráng miệng, đồ uống…) | Chủ quán | B |
| Sắp xếp thứ tự hiển thị nhóm | Chủ quán | B |

### Module 6 — Table Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Thêm/sửa/xóa bàn (số thứ tự, số người tối đa) | Chủ quán | B |
| Sắp xếp khu vực/tầng (tùy chọn) | Chủ quán | T |
| Xem sơ đồ bàn + trạng thái (trống, có khách, đang đợi món, món đã xong) | Phục vụ, Quản lý, Chủ quán | B |
| Chuyển bàn | Phục vụ, Quản lý | B |
| Ghép bàn tính chung hóa đơn | Phục vụ, Quản lý | B |

### Module 7 — Order Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Mở bàn, tạo hóa đơn, nhập số lượng khách | Phục vụ, Quản lý | B |
| Gọi món lần đầu | Phục vụ, Quản lý | B |
| Gọi thêm món (nhiều lần) | Phục vụ, Quản lý | B |
| Đổi món | Phục vụ, Quản lý | B |
| Hủy món (chỉ khi chưa thực hiện) | Phục vụ, Quản lý | B |
| Xem chi tiết hóa đơn, tạm tính | Phục vụ, Quản lý | B |
| Ghi chú món (ít đá, không cay…) | Phục vụ, Quản lý | B |

### Module 8 — Kitchen Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Xem hàng đợi món theo thứ tự thời gian | Bếp | B |
| Lọc theo loại món (món ăn / đồ uống cho quầy pha chế) | Bếp | T |
| Cập nhật món hoàn thành | Bếp | B |
| Xem thời gian chờ của từng món | Bếp | T |
| Báo hết món cho phục vụ | Bếp | B |

### Module 9 — Payment Management
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Tính tiền hóa đơn (cộng món, trừ món đã hủy) | Phục vụ, Quản lý | B |
| Áp giảm giá/khuyến mãi (tùy chọn) | Quản lý, Chủ quán | T |
| Ghi nhận thanh toán (tiền mặt/chuyển khoản) | Phục vụ, Quản lý | B |
| In hóa đơn | Phục vụ, Quản lý | B |
| Giải phóng bàn sau thanh toán | Hệ thống | B |
| Xem lại hóa đơn đã thanh toán | Chủ quán, Quản lý | B |

### Module 10 — Reporting
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Doanh thu theo tháng/quý/năm | Chủ quán | B |
| Số lượng khách theo ca trong tháng/quý/năm | Chủ quán | B |
| Số lượng khách một nhân viên phục vụ trong tuần | Chủ quán, Quản lý | B |
| Số lượng khách của bàn trong tháng/quý/năm | Chủ quán | B |
| Số lượng bán của món trong tháng/quý/năm | Chủ quán | B |
| Số ca làm việc của nhân viên | Chủ quán, Quản lý | B |
| Biểu đồ + xuất/in báo cáo | Chủ quán | B |

### Module 11 — Shift & Schedule Management *(bổ sung)*
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Cấu hình ca (tên ca, giờ bắt đầu, giờ kết thúc) | Chủ quán | B |
| Cấu hình danh mục trình độ vi tính | Chủ quán | B |
| Xem/chỉnh lịch tuần của tất cả nhân viên (7 ngày × số ca/ngày) | Chủ quán | B |
| Thêm/xóa nhân viên khỏi ca, xem ca trống & số người mỗi ca | Chủ quán | B |
| Xem/chỉnh lịch tuần của một nhân viên, đổi trạng thái ca | Chủ quán, Quản lý | B |
| Xếp lịch theo tháng | Chủ quán | B |
| Nhân viên xem lịch của mình theo tuần | Phục vụ, Bếp | B |
| Duyệt số ca làm việc trong tháng | Chủ quán | B |

### Module 12 — Attendance & Payroll *(bổ sung — mức cơ bản)*
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Chấm công: bắt đầu làm / kết thúc làm (lấy giờ hệ thống) | Phục vụ, Bếp, Quản lý | B |
| Duyệt chấm công (một nút duyệt) | Quản lý, Chủ quán | B |
| Xem số ca đã duyệt trong tháng của nhân viên | Chủ quán, Quản lý | B |
| Tính lương = số ca đã duyệt × đơn giá ca | Chủ quán | B |

> Giữ ở mức cơ bản theo yêu cầu: không xử lý ca đêm, tăng ca, đi muộn/về sớm, nghỉ phép, làm tròn giờ. Chấm công chỉ ghi nhận giờ vào/ra và trạng thái duyệt.

### Module 13 — System Configuration *(tùy chọn)*
| Chức năng | Actor | Ưu tiên |
|-----------|-------|---------|
| Cấu hình tham số chung (số ca/ngày, khung giờ mặc định, đơn giá ca) | Chủ quán | T |
| Backup dữ liệu | Chủ quán | T |
| Restore dữ liệu | Chủ quán | T |

## 3. Phạm vi triển khai theo giai đoạn

| Giai đoạn | Module | Ghi chú |
|-----------|--------|---------|
| Phase 1–2 | Nền tảng, Database Design | Theo `09_DevelopmentRoadmap.md` |
| Phase 3 | Module 1, 3 | Xác thực + phân quyền cơ bản |
| Phase 4 | Module 2, 11 | Nhân viên + ca/lịch |
| Phase 5 | Module 4, 5, 6 | Thực đơn + bàn |
| Phase 6 | Module 7, 8 | Gọi món + bếp |
| Phase 7 | Module 9, 12 | Thanh toán + chấm công/lương |
| Phase 8 | Module 10, 13 | Báo cáo + cấu hình/backup |
