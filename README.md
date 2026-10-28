# Hệ thống quản lý quán ăn

Đồ án xây dựng ứng dụng quản lý hoạt động nhà hàng/quán ăn: thực đơn, bàn, gọi món, bếp, thanh toán, nhân sự, chấm công và báo cáo. Giao diện và hướng dẫn sử dụng trong repo này bằng tiếng Việt.

## Công nghệ

- ASP.NET Core MVC trên .NET 10
- Entity Framework Core 10
- SQL Server 2022 chạy trong Docker
- Bootstrap
- Clean Architecture: `Web → Infrastructure → Application → Domain`

## Chức năng chính

| Phân hệ | Nội dung |
|---|---|
| Bán hàng (POS) | Thực đơn, sơ đồ bàn, mở bàn, gọi món, chuyển/gộp bàn |
| Bếp | Hàng đợi món FIFO, nhận chế biến và cập nhật trạng thái món |
| Thanh toán | Tính tiền, giảm giá theo quyền, tiền mặt/chuyển khoản, biên nhận |
| Nhân sự | Nhân viên, tài khoản, ca làm, lịch ca, chấm công và duyệt công |
| Báo cáo | Doanh thu, món bán, khách theo ca/nhân viên/bàn và lương |
| Nhật ký thao tác | Tra cứu thay đổi dữ liệu và lịch sử chuyển/gộp bàn |

Các luồng nghiệp vụ quan trọng gồm lưu giá món tại thời điểm gọi, không cho gọi món hết hoặc thuộc nhóm đã ngừng, chỉ hủy món còn chờ bếp, kiểm tra sức chứa khi mở/gộp bàn và giải phóng bàn cùng giao dịch thanh toán. Món bị xóa khỏi thực đơn được xóa mềm; quản lý có thể khôi phục món vào nhóm đang hoạt động.

## Yêu cầu

- Windows 10/11, macOS hoặc Linux
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) hoặc Docker Engine với Docker Compose
- Tối thiểu 2 GB RAM khả dụng cho SQL Server container

## Chạy lần đầu

Các lệnh sau dùng PowerShell từ thư mục gốc của repo.

### 1. Cấu hình mật khẩu SQL Server

```powershell
Copy-Item .\docker\.env.example .\docker\.env
notepad .\docker\.env
```

Trong `docker/.env`, thay `REPLACE_WITH_A_STRONG_PASSWORD` bằng mật khẩu riêng đáp ứng yêu cầu của SQL Server (ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt). Không chia sẻ hoặc commit file `.env`.

### 2. Khởi tạo cơ sở dữ liệu

```powershell
docker compose --env-file .\docker\.env -f .\docker\docker-compose.yml up -d --build
docker compose --env-file .\docker\.env -f .\docker\docker-compose.yml logs -f sqlserver
```

Đợi log báo khởi tạo database hoàn tất, rồi nhấn `Ctrl+C` để thoát theo dõi log; container vẫn chạy nền. Trên lần khởi tạo đầu tiên, SQL Server chạy các script theo thứ tự:

1. `database/01_schema.sql` — database, bảng và ràng buộc.
2. `database/02_seed.sql` — dữ liệu mẫu.
3. `database/03_views.sql` — các view báo cáo và vận hành.
4. `database/04_audit.sql` — trigger nhật ký thao tác.
5. `database/05_rules.sql` — trigger quy tắc nghiệp vụ.

Database được lưu trong Docker volume để giữ dữ liệu giữa các lần chạy. Các script khởi tạo không chạy lại trên database đã tồn tại.

### 3. Cấu hình kết nối ứng dụng

Mật khẩu kết nối được lưu bằng .NET User Secrets, không ghi vào file cấu hình trong repo:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=RestaurantManagement;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True" --project .\src\RestaurantManagement.Web
```

Thay `YOUR_SQL_PASSWORD` bằng đúng mật khẩu vừa đặt trong `docker/.env`.

### 4. Chạy ứng dụng

```powershell
dotnet run --project .\src\RestaurantManagement.Web
```

Mở [http://localhost:5010](http://localhost:5010). Có thể kiểm tra kết nối database tại [http://localhost:5010/health/db](http://localhost:5010/health/db).

## Tài khoản demo

Database seed sẵn các tài khoản:

| Tên đăng nhập | Mật khẩu ban đầu | Vai trò |
|---|---|---|
| `admin` | `Admin@123` | Quản trị hệ thống |
| `manager` | `Admin@123` | Quản lý quán |

Đây là thông tin đăng nhập **chỉ dành cho database demo cục bộ**. Đổi mật khẩu sau khi đăng nhập; không dùng các tài khoản này cho môi trường triển khai thật.

## Gợi ý kiểm tra khi chấm đồ án

1. Đăng nhập `manager` để xem **Bán hàng (POS)**: thực đơn, bàn, gọi món và thao tác quản lý theo quyền.
2. Mở một bàn trống, gọi một món; kiểm tra món xuất hiện trong hàng đợi bếp.
3. Đăng nhập `admin` hoặc dùng một tài khoản có vai trò `KITCHEN` để nhận món và đánh dấu hoàn thành.
4. Thanh toán bằng tài khoản `manager`; sau thanh toán, kiểm tra bàn được giải phóng và biên nhận được tạo.
5. Mở **Báo cáo** để xem doanh thu/món bán, và **Nhật ký thao tác** để tra cứu thay đổi.
6. Các màn hình nhân sự, ca làm, lịch ca, chấm công và duyệt công dành cho quản lý; quyền chi tiết được mô tả trong `docs/03_UserRoles.md`.

Trạng thái dữ liệu thay đổi khi chạy thử được lưu trong database Docker. Nếu cần database sạch, lệnh dưới sẽ **xóa volume và toàn bộ dữ liệu**; chỉ chạy khi chấp nhận mất dữ liệu hiện có:

```powershell
docker compose --env-file .\docker\.env -f .\docker\docker-compose.yml down -v
docker compose --env-file .\docker\.env -f .\docker\docker-compose.yml up -d --build
```

## Cấu trúc repository

```text
database/   SQL schema, seed, views, audit triggers và quy tắc nghiệp vụ
docker/     Dockerfile, Docker Compose và hướng dẫn SQL Server
docs/       Yêu cầu, thiết kế CSDL, vai trò và nhật ký triển khai
src/        Solution ASP.NET Core MVC và các project Clean Architecture
```

## Tài liệu tham khảo trong repo

- `docs/02_BusinessRequirements.md` — yêu cầu nghiệp vụ.
- `docs/03_UserRoles.md` — vai trò và phạm vi quyền.
- `docs/04_UseCases.md` — ca sử dụng.
- `docs/sequence.md` — biểu đồ tuần tự các luồng nghiệp vụ chính.
- `docs/11_DatabaseSchemaDesign.md` — thiết kế cơ sở dữ liệu.
- `docs/09_DevelopmentRoadmap.md` — roadmap.
- `database/README.md` — thứ tự và nội dung SQL scripts.
- `docker/README.md` — vận hành SQL Server bằng Docker.
- `src/README.md` — cấu trúc solution và ghi chú kỹ thuật.

## Ghi chú dữ liệu và bảo mật

- Repo không chứa file `.env`, mật khẩu SQL cá nhân, backup database `.bak` hoặc PDF đề bài.
- `database/02_seed.sql` chứa dữ liệu demo. Khi chạy thử, database có thông tin tài khoản mẫu và dữ liệu nghiệp vụ; không đưa dữ liệu cá nhân/thật vào môi trường công khai.
- Script SQL là nguồn quản lý schema hiện tại; dự án chưa sử dụng EF Core Migrations.
- Backup và hướng dẫn backup/restore xem `database/backup/README.txt` và `docker/README.md`.
