# Database Requirements

> Đề xuất thiết kế CSDL cho hệ thống quản lý quán ăn, xuất phát từ 2 quy trình nghiệp vụ và các báo cáo bắt buộc trong `10_Quan-ly-quan-an.pdf`. Không bao gồm mã nguồn.

## 1. Danh sách bảng đề xuất

| # | Bảng | Vai trò |
|---|------|---------|
| 1 | `Role` (VaiTro) | Loại người dùng: Chủ quán, Quản lý, Phục vụ, Bếp |
| 2 | `User` (TaiKhoan) | Tài khoản đăng nhập, liên kết nhân viên |
| 3 | `Permission` + `RolePermission` | Phân quyền chi tiết theo chức năng *(tùy chọn)* |
| 4 | `Employee` (NhanVien) | Hồ sơ nhân viên |
| 5 | `Skill` (TrinhDoViTinh) | Danh mục trình độ vi tính (Word, Excel, Cài phần mềm, Khác) |
| 6 | `EmployeeSkill` | Quan hệ nhiều-nhiều nhân viên ↔ trình độ |
| 7 | `Shift` (Ca) | Cấu hình ca: tên, giờ bắt đầu, giờ kết thúc |
| 8 | `EmployeeShift` | Ca mà nhân viên **có thể** làm việc |
| 9 | `Schedule` (LichLamViec) | Phân ca theo tháng/ngày |
| 10 | `Attendance` (ChamCong) | Giờ vào/ra và trạng thái duyệt |
| 11 | `UniformItem` + `UniformIssue` | Dụng cụ/trang phục đã cấp và đã trả |
| 12 | `Employee.ShiftRate` | Đơn giá ca (dùng để tính lương, không cần bảng lương riêng) |
| 13 | `DiningTable` (Ban) | Số bàn, số người tối đa, trạng thái |
| 14 | `Category` (LoaiMon) | Nhóm món |
| 15 | `MenuItem` (MonAn) | Món ăn/nước uống, giá, trạng thái còn/hết |
| 16 | `Invoice` (HoaDon) | Hóa đơn của lượt khách |
| 17 | `InvoiceTable` | Liên kết hóa đơn ↔ bàn (phục vụ ghép bàn/chuyển bàn) |
| 18 | `InvoiceDetail` (ChiTietHoaDon) | Từng dòng món, số lượng, ghi chú, trạng thái |
| 19 | `TableTransferLog` | Lịch sử chuyển bàn |
| 20 | `Payment` (ThanhToan) | Thông tin thanh toán |
| 21 | `SystemParameter` | Tham số cấu hình (số ca/ngày, mốc tính ca báo cáo…) |

## 2. Chi tiết các bảng chính

### 2.1 `User`
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK, identity) | |
| Username | nvarchar(50) | UNIQUE, NOT NULL |
| PasswordHash | nvarchar(255) | NOT NULL |
| RoleId | int (FK → Role) | NOT NULL |
| EmployeeId | int (FK → Employee, NULL) | Tài khoản gắn với nhân viên |
| IsActive | bit | Khóa tài khoản khi nghỉ việc |
| LastLoginAt | datetime2 NULL | |

### 2.2 `Employee`
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| FullName | nvarchar(100) | Họ tên |
| Gender | tinyint/nvarchar(10) | Giới tính |
| DateOfBirth | date | Ngày sinh |
| Position | nvarchar(50) | Vị trí: Phục vụ / Pha chế / Quản lý / Tiếp tân |
| Phone | nvarchar(20) | |
| Address | nvarchar(255) | |
| IdentityNumber | nvarchar(20) | CMND/CCCD, UNIQUE |
| StartDate | date | Thời gian bắt đầu làm việc |
| HourlyRate / ShiftRate | decimal(18,2) | Đơn giá ca phục vụ tính lương |
| Status | tinyint | Đang làm / Đã nghỉ / Tạm nghỉ |
| EndDate | date NULL | Ngày nghỉ việc |
| Note | nvarchar(255) | |

### 2.3 `Shift` (Ca) — cấu hình được
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| Name | nvarchar(50) | VD: Ca sáng, Ca chiều |
| StartTime | time | 09:00 |
| EndTime | time | 16:00 |
| IsActive | bit | Ngừng dùng thay vì xóa |

### 2.4 `EmployeeShift` (ca có thể làm)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| EmployeeId | int (PK, FK) | |
| ShiftId | int (PK, FK) | |

### 2.5 `Schedule` (lịch làm việc)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| EmployeeId | int (FK) | |
| ShiftId | int (FK) | |
| WorkDate | date | Lịch lưu theo tháng, kiểm tra theo ngày |
| Status | tinyint | Chưa duyệt / Đã duyệt / Vắng |
| ApprovedBy | int NULL | Người duyệt số ca |
| Note | nvarchar(255) | |

**Ràng buộc:** UNIQUE (EmployeeId, ShiftId, WorkDate); chặn 2 ca chồng giờ trong cùng ngày (kiểm tra ở tầng nghiệp vụ).

### 2.6 `Attendance` (chấm công — mức cơ bản)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| EmployeeId | int (FK) | |
| ShiftId | int (FK) | Ca đã được xếp trong ngày |
| WorkDate | date | |
| CheckInTime | datetime2 | Bắt đầu làm (giờ hệ thống) |
| CheckOutTime | datetime2 NULL | Kết thúc làm (giờ hệ thống) |
| Status | tinyint | Chờ duyệt / Đã duyệt |
| ApprovedBy | int NULL | Quản lý duyệt |

> Chỉ cần 4 thao tác: chấm vào, chấm ra, duyệt, đếm số ca. Không lưu đi muộn/về sớm, tăng ca, nghỉ phép. Ràng buộc: UNIQUE (EmployeeId, ShiftId, WorkDate).

### 2.7 `DiningTable` (Bàn)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| TableNumber | nvarchar(10) | Số thứ tự bàn, UNIQUE |
| MaxSeats | int | Số người tối đa |
| Area | nvarchar(50) NULL | Khu vực (tùy chọn) |
| Status | tinyint | Trống / Có khách / Đang đợi món / Món đã xong |
| IsActive | bit | |

> `Status` có thể lưu trực tiếp (đơn giản, đúng yêu cầu hiển thị) **hoặc** suy ra từ hóa đơn + trạng thái món. Nếu lưu, cần cập nhật đồng bộ ở các luồng mở bàn / gọi món / bếp xong / thanh toán.

### 2.8 `Category` và `MenuItem`
`Category`: Id (PK), Name nvarchar(100), DisplayOrder int, IsActive bit.

`MenuItem`:
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| Code | nvarchar(20) | Mã món, UNIQUE |
| Name | nvarchar(200) | Tên món |
| Price | decimal(18,2) | Giá bán |
| Unit | nvarchar(20) | Đơn vị (phần, ly, chai…) |
| CategoryId | int (FK → Category) | |
| IsAvailable | bit | Còn/hết món (BR-05) |
| ImageUrl | nvarchar(255) NULL | |

### 2.9 `Invoice` (Hóa đơn)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| InvoiceCode | nvarchar(20) | Mã hóa đơn, UNIQUE |
| CustomerCount | int | Số lượng khách (bắt buộc — dùng cho báo cáo) |
| OpenedBy | int (FK → Employee) | Nhân viên tiếp nhận |
| OpenedAt | datetime2 | |
| ClosedAt | datetime2 NULL | |
| ShiftId | int (FK → Shift) NULL | Ca ghi nhận, phục vụ báo cáo theo ca |
| SubTotal | decimal(18,2) | Tổng trước giảm giá |
| Discount | decimal(18,2) | |
| TotalAmount | decimal(18,2) | Thành tiền |
| Status | tinyint | Đang phục vụ / Đã thanh toán / Đã hủy |
| Note | nvarchar(255) | |

### 2.10 `InvoiceTable` (liên kết hóa đơn ↔ bàn)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| InvoiceId | int (PK, FK) | |
| TableId | int (PK, FK) | |
| JoinedAt | datetime2 | Thời điểm bàn được gắn vào hóa đơn |
| IsActive | bit | Bàn đã tách/rời hóa đơn chưa |

> Bảng này giải quyết đồng thời **ghép bàn** (1 hóa đơn – N bàn) và **chuyển bàn** (đóng liên kết cũ, mở liên kết mới) mà vẫn giữ được báo cáo "số khách của bàn" (BR-03, BR-04, BR-07).

### 2.11 `InvoiceDetail` (Chi tiết hóa đơn)
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| InvoiceId | int (FK) | |
| MenuItemId | int (FK) | |
| Quantity | int | |
| UnitPrice | decimal(18,2) | Giá tại thời điểm gọi (không đổi khi giá món thay đổi) |
| Note | nvarchar(255) | Ghi chú món (ít đá, không cay…) |
| Status | tinyint | Đang chờ / Đang chế biến / Hoàn thành / Đã hủy |
| OrderedAt | datetime2 | Dùng sắp xếp hàng đợi bếp FIFO (BR-06) |
| CompletedAt | datetime2 NULL | Bếp cập nhật |
| CancelReason | nvarchar(255) NULL | Chỉ cho hủy khi Status = Đang chờ (BR-02) |
| CancelledBy | int NULL | |

### 2.12 `TableTransferLog`
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| InvoiceId | int (FK) | |
| FromTableId | int (FK) | |
| ToTableId | int (FK) | |
| TransferredAt | datetime2 | |
| TransferredBy | int (FK → Employee) | |

### 2.13 `Payment`
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Id | int (PK) | |
| InvoiceId | int (FK) | |
| Method | tinyint | Tiền mặt / Chuyển khoản |
| Amount | decimal(18,2) | |
| PaidAt | datetime2 | |
| PaidBy | int (FK → Employee) | |

### 2.14 `UniformItem` / `UniformIssue`
`UniformItem`: Id, Name, Unit, QuantityInStock.
`UniformIssue`: Id, EmployeeId, UniformItemId, IssuedAt, ReturnedAt NULL, Quantity, Status (Đang giữ / Đã trả / Mất).

### 2.15 `SystemParameter`
| Field | Kiểu dữ liệu | Ghi chú |
|-------|--------------|---------|
| Key | nvarchar(50) (PK) | VD `ShiftsPerDay`, `SalaryRounding` |
| Value | nvarchar(255) | |
| Description | nvarchar(255) | |

## 3. Quan hệ chính

- `Role` 1 — N `User`
- `Employee` 1 — N `Schedule`, 1 — N `Attendance`, 1 — N `UniformIssue`
- `Employee` N — N `Shift` (qua `EmployeeShift` = ca có thể làm)
- `Shift` 1 — N `Schedule`, 1 — N `Attendance`
- `Category` 1 — N `MenuItem`
- `Invoice` 1 — N `InvoiceDetail`, 1 — N `Payment`, 1 — N `TableTransferLog`
- `Invoice` N — N `DiningTable` (qua `InvoiceTable`)
- `Employee` 1 — N `Invoice` (nhân viên tiếp nhận qua `OpenedBy`)

## 4. Truy vết báo cáo → dữ liệu nguồn

| Báo cáo | Nguồn dữ liệu | Lưu ý |
|---------|---------------|-------|
| Doanh thu tháng/quý/năm | `Invoice.TotalAmount` + `ClosedAt` (hoặc `Payment.PaidAt`) | Chỉ hóa đơn `Đã thanh toán` |
| Số khách theo ca | `Invoice.CustomerCount` + `Invoice.ShiftId` | Cần chốt mốc gán ca (mở bàn hay thanh toán) |
| Số khách 1 nhân viên/tuần | `Invoice.CustomerCount` + `OpenedBy` + `ClosedAt` | Lọc theo tuần |
| Số khách của bàn | `Invoice.CustomerCount` qua `InvoiceTable` | Ghép bàn vẫn tách theo từng bàn |
| Số lượng bán của món | `SUM(InvoiceDetail.Quantity)` (loại `Đã hủy`) | Kèm doanh thu theo món |
| Số ca nhân viên | `Schedule` (đã duyệt) hoặc `Attendance.Status = Đã duyệt` | Thống nhất một nguồn với tính lương |

## 5. Ghi chú thiết kế & rủi ro

1. **Ca làm việc phải ở bảng cấu hình.** Đề bài mâu thuẫn 3 ca (8–13, 12–17, 17–22) và 2 ca (9–16, 16–22) → không hard-code, dùng `Shift` + dữ liệu mẫu.
2. **Giá món phải lưu tại thời điểm gọi** (`InvoiceDetail.UnitPrice`) để hóa đơn cũ không bị thay đổi khi giá thực đơn đổi.
3. **Không xóa cứng dữ liệu nghiệp vụ.** Dùng `IsActive`/`Status` cho bàn, món, nhân viên, ca để báo cáo lịch sử còn đúng.
4. **`DiningTable.Status` nên suy ra từ dữ liệu** (hóa đơn + trạng thái món) ở tầng service để tránh lệch trạng thái; nếu lưu vật lý thì phải cập nhật trong cùng transaction với các thao tác mở bàn/gọi món/bếp xong/thanh toán.
5. **Ghép bàn là thao tác trên `InvoiceTable`, không phải sửa `InvoiceDetail`** — tránh mất dấu bàn gốc khi báo cáo.
6. **Duyệt chấm công là điều kiện tính lương** → chỉ đếm `Attendance.Status = Đã duyệt`; lương tính trực tiếp từ số ca × `Employee.ShiftRate`, không cần bảng lương riêng.
7. **CMND/CCCD và Username cần UNIQUE**; thêm index cho `Schedule(WorkDate)`, `Attendance(WorkDate)`, `Invoice(ClosedAt)`, `InvoiceDetail(InvoiceId, Status)` để báo cáo nhanh.

## 6. Bảng thiết kế lớp (theo mẫu trong đề bài)

Đề bài có bảng trống `Tên lớp / Tên thuộc tính / Kiểu dữ liệu` với 2 lớp gợi ý (`NhanVien`: Họ tên, Địa chỉ, Ca làm việc; `Ban`: Mã bàn, Số người tối đa). Bảng dưới đây điền mẫu cho các lớp cốt lõi:

| Tên lớp | Tên thuộc tính | Kiểu dữ liệu |
|---------|----------------|--------------|
| NhanVien | MaNV | int |
| NhanVien | HoTen | nvarchar(100) |
| NhanVien | GioiTinh | nvarchar(10) |
| NhanVien | NgaySinh | date |
| NhanVien | ViTri | nvarchar(50) |
| NhanVien | DienThoai | nvarchar(20) |
| NhanVien | DiaChi | nvarchar(255) |
| NhanVien | CMND | nvarchar(20) |
| NhanVien | NgayBatDauLam | date |
| NhanVien | TrangThai | tinyint |
| Ban | MaBan | int |
| Ban | SoThuTu | nvarchar(10) |
| Ban | SoNguoiToiDa | int |
| Ban | TrangThai | tinyint |
| Ca | MaCa | int |
| Ca | TenCa | nvarchar(50) |
| Ca | GioBatDau | time |
| Ca | GioKetThuc | time |
| LichLamViec | MaLich | int |
| LichLamViec | MaNV | int |
| LichLamViec | MaCa | int |
| LichLamViec | NgayLam | date |
| LichLamViec | TrangThai | tinyint |
| ChamCong | MaChamCong | int |
| ChamCong | MaNV | int |
| ChamCong | MaCa | int |
| ChamCong | GioVao | datetime2 |
| ChamCong | GioRa | datetime2 |
| ChamCong | TrangThaiDuyet | tinyint |
| MonAn | MaMon | int |
| MonAn | TenMon | nvarchar(200) |
| MonAn | DonGia | decimal(18,2) |
| MonAn | MaLoai | int |
| MonAn | ConBan | bit |
| HoaDon | MaHoaDon | int |
| HoaDon | SoLuongKhach | int |
| HoaDon | MaNV | int |
| HoaDon | ThoiGianMo | datetime2 |
| HoaDon | ThoiGianDong | datetime2 |
| HoaDon | TongTien | decimal(18,2) |
| HoaDon | TrangThai | tinyint |
| ChiTietHoaDon | MaChiTiet | int |
| ChiTietHoaDon | MaHoaDon | int |
| ChiTietHoaDon | MaMon | int |
| ChiTietHoaDon | SoLuong | int |
| ChiTietHoaDon | DonGia | decimal(18,2) |
| ChiTietHoaDon | GhiChu | nvarchar(255) |
| ChiTietHoaDon | TrangThai | tinyint |
| ChiTietHoaDon | ThoiGianDat | datetime2 |
