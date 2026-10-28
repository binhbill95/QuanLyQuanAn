# Implementation Log — Phiên 2026-10-27

> Nối tiếp `12_ImplementationLog.md` (2026-10-26: thiết kế CSDL + Docker).
> Phiên này: **Phase 1 — Project Initialization** + scaffold Entity Framework Core từ database thật.

## 1. Thông tin phiên

| Hạng mục | Nội dung |
|----------|----------|
| Ngày | 2026-10-27 |
| Phạm vi | Khởi tạo solution Clean Architecture, scaffold DbContext/entity, cấu hình DI, kiểm tra kết nối DB |
| Kết quả | Solution 4 project build **0 warning / 0 error**, chạy được, `/health/db` trả về `healthy` |
| Môi trường | .NET SDK **10.0.401** (duy nhất), EF Core 10.0.14, SQL Server 2022 container (healthy) |
| Thời lượng | ~35 phút (tạo project → scaffold → build → chạy thử) |

## 2. Kiểm tra môi trường (bước 0)

| Kiểm tra | Kết quả | Ảnh hưởng |
|----------|---------|-----------|
| `dotnet --list-sdks` | Chỉ có SDK **10.0.303** và **10.0.401** | Không tạo được project `net9.0` từ template |
| `dotnet --list-runtimes` | Có `Microsoft.NETCore.App 9.0.20` nhưng **không có** `Microsoft.AspNetCore.App 9.x` | Ứng dụng `net9.0` không chạy được trên máy này |
| `dotnet new classlib -f net9.0` | Lỗi `exit code 127` (template không hỗ trợ TFM cũ) | Phải chọn `net10.0` |
| Docker | `restaurant-sqlserver` healthy, DB đã có dữ liệu | Dùng làm nguồn scaffold |

→ **Quyết định:** dùng **net10.0** cho toàn solution, ghi rõ trong `07_TechnologyStack.md` và `src/README.md` kèm hướng dẫn hạ về 9 nếu môn học bắt buộc.

## 3. Dòng thời gian thực hiện

| # | Bước | Kết quả |
|---|------|---------|
| 1 | Kiểm tra SDK/runtime/Docker | Xác định chỉ dùng được .NET 10 |
| 2 | `dotnet new sln` + 3 classlib + 1 mvc | 4 project, solution `RestaurantManagement.slnx` |
| 3 | Thêm reference theo Clean Architecture | `Application → Domain`, `Infrastructure → Application, Domain`, `Web → cả 3` |
| 4 | Cài package | EF Core SqlServer 10.0.14, EF Core Design 10.0.14, Configuration.Abstractions |
| 5 | Cài `dotnet-ef` global | Version 10.0.14 |
| 6 | Scaffold database-first | 25 entity bảng + 10 entity view (keyless) + `RestaurantDbContext` |
| 7 | Chuyển entity về Domain | Namespace `RestaurantManagement.Domain.Entities`, xóa `Class1.cs` |
| 8 | Viết `DependencyInjection.cs` | `AddInfrastructure()` đăng ký DbContext + retry policy |
| 9 | Cấu hình Web | `Program.cs` gọi DI, `appsettings.json` có `DefaultConnection`, thêm `/health/db` |
| 10 | Build | **0 warning, 0 error** (sau khi ghim `Pkcs` 10.0.14) |
| 11 | Chạy thử | `/` = 200, `/health/db` = `{"status":"healthy","employees":4}` |
| 12 | Viết README + nhật ký | Hoàn tất |

## 4. Cấu trúc solution

```
src/
├── RestaurantManagement.slnx
├── RestaurantManagement.Domain/          ← không phụ thuộc project nào
│   └── Entities/                         ← 35 lớp (25 bảng + 10 view)
├── RestaurantManagement.Application/     ← không phụ thuộc EF
│   ├── Abstractions/ICurrentUserService.cs
│   └── Common/PagedResult.cs
├── RestaurantManagement.Infrastructure/
│   ├── Persistence/RestaurantDbContext.cs
│   └── DependencyInjection.cs
└── RestaurantManagement.Web/
    ├── Program.cs                        ← composition root + /health/db
    ├── Controllers/ Views/ wwwroot/      ← MVC template
    └── appsettings.json                  ← ConnectionStrings:DefaultConnection
```

**Chiều phụ thuộc:** `Web → Infrastructure → Application → Domain` (không có vòng lặp).

## 5. Quyết định kỹ thuật

| # | Quyết định | Lý do | Hệ quả |
|---|-----------|-------|--------|
| 1 | Target **net10.0** thay vì net9.0 | Máy chỉ có SDK/runtime 10; template net9.0 lỗi | Cập nhật docs/02, docs/07; hướng dẫn hạ version nếu cần |
| 2 | **Database-first** (scaffold) thay vì code-first | CSDL đã hoàn chỉnh, có 10 view + trigger; scaffold đảm bảo khớp 100% | Chưa có migration; nếu đổi sang code-first phải tạo baseline migration |
| 3 | Entity đặt trong **Domain**, DbContext trong Infrastructure | Đúng Clean Architecture, Domain không biết EF | Đã di chuyển file và giữ namespace Domain |
| 4 | 10 **view** scaffold thành entity `Keyless` | Dùng lại được cho báo cáo (Phase 8) mà không viết SQL thủ công | Có thêm `Vw*` trong Domain; không dùng để ghi dữ liệu |
| 5 | Giữ `RestaurantDbContext` là `partial` + cấu hình Fluent trong `OnModelCreating` | Theo output chuẩn của scaffold, dễ mở rộng | Khi thêm entity mới nên tách `IEntityTypeConfiguration` |
| 6 | Ghim `System.Security.Cryptography.Pkcs` **10.0.14** | `Microsoft.Data.SqlClient 6.1.7` kéo bản 9.0.11 có lỗ hổng (NU1902/NU1903) | Build sạch cảnh báo |
| 7 | `/health/db` tạm đặt trong `Program.cs` | Kiểm chứng nhanh Phase 1 | Phase sau chuyển sang `AddHealthChecks()` + `HealthCheckService` |
| 8 | Chưa cài ASP.NET Identity | Thuộc Phase 3 | Hiện chưa có bảng `AspNetUsers`; xem docs/12 mục 9 |

## 6. Kết quả kiểm chứng

### 6.1 Build

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 6.2 Runtime

| Kiểm tra | Lệnh | Kết quả |
|----------|------|---------|
| Trang chủ | `GET /` | `200 OK` |
| Kết nối DB | `GET /health/db` | `{"status":"healthy","database":"RestaurantManagement","employees":4}` |
| Cổng chạy | `launchSettings.json` | `http://localhost:5010` |

### 6.3 Đối chiếu schema ↔ entity

| Hạng mục | Số lượng | Ghi chú |
|----------|:--------:|---------|
| Bảng trong DB | 25 | Khớp `docs/11` |
| Entity bảng | 25 | `Domain/Entities` |
| View trong DB | 10 | Khớp `03_views.sql` |
| Entity view | 10 | `Vw*.cs`, keyless |
| Tổng file entity | 35 | `RestaurantDbContext` 37,3 KB |

## 7. Lỗi gặp phải & cách xử lý

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|-----|-------------|-----------|
| 1 | `dotnet new classlib -f net9.0` → exit 127 | SDK 10 template không hỗ trợ `net9.0` | Tạo mặc định `net10.0`, ghi chú phương án hạ TFM |
| 2 | Scaffold đặt entity trong `Infrastructure/Entities` | Mặc định `-o` tính theo project hiện tại | Dùng `Move-Item` sang `Domain/Entities`, giữ nguyên namespace đã chỉ định |
| 3 | Cảnh báo NU1902 + NU1903 (Pkcs 9.0.11) | `Microsoft.Data.SqlClient` kéo bản cũ có lỗ hổng | Thêm PackageReference trực tiếp `Pkcs` 10.0.14 ở Infrastructure và Web |
| 4 | `dotnet run` bỏ qua `ASPNETCORE_URLS`, chạy cổng 5010 | `launchSettings.json` được ưu tiên | Chấp nhận cổng 5010; khi cần đổi thì sửa `launchSettings.json` |
| 5 | Cảnh báo "Failed to determine the https port" | Dev chỉ chạy HTTP nhưng vẫn gọi `UseHttpsRedirection()` | Chỉ gọi `UseHttpsRedirection()` ngoài môi trường Development |
| 6 | Entity view sinh thêm 10 lớp ngoài dự kiến | Scaffold lấy toàn bộ đối tượng trong DB | Giữ lại (hữu ích cho báo cáo), ghi chú rõ là keyless và chỉ đọc |

## 8. Tệp đã tạo / cập nhật

| Tệp | Trạng thái | Ghi chú |
|-----|-----------|---------|
| `src/RestaurantManagement.slnx` | Mới | Solution 4 project |
| `src/RestaurantManagement.Domain/Entities/*.cs` | Mới (35) | 25 bảng + 10 view |
| `src/RestaurantManagement.Application/Common/PagedResult.cs` | Mới | Kiểu phân trang dùng chung |
| `src/RestaurantManagement.Application/Abstractions/ICurrentUserService.cs` | Mới | Hợp đồng cho Phase 3 |
| `src/RestaurantManagement.Infrastructure/Persistence/RestaurantDbContext.cs` | Mới | DbContext + Fluent config |
| `src/RestaurantManagement.Infrastructure/DependencyInjection.cs` | Mới | `AddInfrastructure()` |
| `src/RestaurantManagement.Web/Program.cs` | Cập nhật | DI + `/health/db` |
| `src/RestaurantManagement.Web/appsettings.json` | Cập nhật | Chuỗi kết nối |
| `src/README.md` | Mới | Hướng dẫn chạy + ghi chú net10 |
| `.gitignore` | Mới | Bỏ `bin/obj`, `.env`, `.bak` |
| `docs/07_TechnologyStack.md` | Cập nhật | .NET 10 + ghi chú phiên bản |
| `docs/02_BusinessRequirements.md` | Cập nhật | NFR-06 theo .NET 10 |
| `docs/09_DevelopmentRoadmap.md` | Cập nhật | Trạng thái phase |
| `docs/13_ImplementationLog_2026-10-27.md` | Mới | Nhật ký này |

> **Phiên tiếp theo:** `15_ImplementationLog_2026-10-27_Phase4.md` (quản lý nhân viên).

## 9. Cách chạy

```bash
# 1. SQL Server
cd docker
docker compose up -d
docker compose ps          # chờ healthy

# 2. Ứng dụng
cd ../src
dotnet build
dotnet run --project RestaurantManagement.Web

# 3. Kiểm tra
curl http://localhost:5010/health/db
```

## 10. Việc còn lại

| # | Việc | Phase | Ưu tiên |
|---|------|:-----:|:-------:|
| 1 | Tách cấu hình EF sang `IEntityTypeConfiguration` cho 25 bảng | 2 | Trung bình |
| 2 | Viết repository + service mẫu (Employee) theo pattern | 4 | Cao |
| 3 | Cài ASP.NET Identity + `ApplicationUser` + seed role | 3 | Cao |
| 4 | Chuyển `/health/db` sang HealthChecks chuẩn | 1 | Thấp |
| 5 | Tạo unit test project + test DbContext | 2 | Trung bình |
| 6 | Quyết định net9 hay net10 với giảng viên | — | Cao |
| 7 | DTO + AutoMapper/Mapster cho tầng Application | 4 | Trung bình |
| 8 | Cấu hình logging/Serilog và xử lý lỗi toàn cục | 3 | Thấp |

## 11. Ghi chú khi cập nhật tài liệu

1. Schema đổi → chạy lại scaffold (hoặc migration), sau đó ghi vào mục 3 của file này.
2. Đổi TFM/package → cập nhật `docs/07`, `src/README.md` và mục 5 file này.
3. Mọi kết quả kiểm chứng ghi **kỳ vọng vs thực tế**, kể cả khi đạt.
4. Không commit `appsettings.json` chứa mật khẩu thật; dùng `appsettings.Development.json` hoặc user-secrets (xem TODO).
