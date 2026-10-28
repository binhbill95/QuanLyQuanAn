# Technology Stack

| Thành phần | Công nghệ |
|------------|-----------|
| Backend | ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2022 |
| Database Runtime | Docker |
| Authentication | Cookie Authentication + `PasswordHasher` của ASP.NET Identity (Phase 3 ✅) |
| IDE | Visual Studio 2022 / VS Code |

> **Ghi chú phiên bản:** tài liệu ban đầu ghi *ASP.NET Core 9*. Máy phát triển hiện chỉ cài .NET SDK 10 và không có template/runtime ASP.NET Core 9, nên solution dùng **net10.0**.
> Nếu môn học bắt buộc .NET 9: đổi `TargetFramework` của 4 project thành `net9.0` và hạ các gói EF Core / `Microsoft.Extensions.*` về `9.x` (xem `docs/13_ImplementationLog_2026-10-27.md`).

> **Ghi chú xác thực:** dùng cookie authentication + `PasswordHasher<AppUser>` (PBKDF2-HMAC-SHA512) trên bảng `AppUser`/`Role` sẵn có, **không** cài bảng `AspNet*`.
> Lý do và phương án chuyển sang ASP.NET Identity đầy đủ: xem `docs/14_ImplementationLog_2026-10-27_Phase3.md` mục 5.
