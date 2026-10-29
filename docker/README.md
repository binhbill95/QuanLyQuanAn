# Docker — SQL Server 2022

## Cấu trúc

| Tệp | Vai trò |
|-----|---------|
| `Dockerfile` | Dựa trên `mcr.microsoft.com/mssql/server:2022-latest`, thêm entrypoint tự chạy script |
| `entrypoint.sh` | Khởi động SQL Server → chờ sẵn sàng → chạy `*.sql` trong `/docker-entrypoint-initdb.d` (một lần) |
| `docker-compose.yml` | Service `sqlserver`, volume dữ liệu, volume backup, healthcheck |
| `.env.example` | Mẫu biến môi trường (copy thành `.env`) |

## Chạy

```bash
cd docker
cp .env.example .env      # Windows: Copy-Item .env.example .env
# Sửa .env và thay REPLACE_WITH_A_STRONG_PASSWORD bằng mật khẩu riêng
docker compose up -d --build
docker compose logs -f sqlserver
```

Lần chạy đầu tiên sẽ tự tạo database + seed + view + trigger. Các lần sau **bỏ qua** init để không ghi đè dữ liệu.

> Compose chỉ bind cổng SQL Server vào `127.0.0.1` trên máy demo. Không đổi địa chỉ bind thành `0.0.0.0` hoặc mở cổng 1433 trên router/firewall. Seed chứa tài khoản demo công khai; chỉ dùng database với dữ liệu giả lập, không triển khai cấu hình này cho ứng dụng/dữ liệu production.

## Kiểm tra

```bash
docker exec -it restaurant-sqlserver bash -lc \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
   -Q "SELECT name FROM sys.tables ORDER BY name"'
```

## Kết nối

```
Server=localhost,1433;Database=RestaurantManagement;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=True
```

## Reset dữ liệu (xóa sạch)

```bash
docker compose down -v      # xóa volume restaurant-mssql-data
docker compose up -d --build
```

## Backup / Restore

```bash
# Backup
docker exec -it restaurant-sqlserver bash -lc \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
   -Q "BACKUP DATABASE RestaurantManagement TO DISK=N''/var/opt/mssql/backup/restaurant.bak'' WITH INIT"'

# Restore
docker exec -it restaurant-sqlserver bash -lc \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
   -Q "RESTORE DATABASE RestaurantManagement FROM DISK=N''/var/opt/mssql/backup/restaurant.bak'' WITH REPLACE"'
```

File `.bak` nằm ở `../database/backup/` trên host.

## Lưu ý

- Cần ≥ 2 GB RAM cho container.
- `docker compose` yêu cầu `MSSQL_SA_PASSWORD` trong `.env`; không commit `.env`.
- Nếu cổng local 1433 bận, đổi `MSSQL_PORT` trong `.env` (ví dụ 14330) và cập nhật connection string. Cổng vẫn chỉ bind vào `127.0.0.1`.
- Image SQL Server không có cơ chế init chuẩn như MySQL/Postgres — `entrypoint.sh` trong repo này xử lý việc đó.
- Máy ARM (Apple Silicon): thêm `platform: linux/amd64` cho service.
