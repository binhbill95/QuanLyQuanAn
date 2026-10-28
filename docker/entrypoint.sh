#!/usr/bin/env bash
# =====================================================================================
# Restaurant Management System — SQL Server 2022 container entrypoint
# Nhiệm vụ:
#   1. Khởi động SQL Server
#   2. Chờ server sẵn sàng
#   3. Nếu database RestaurantManagement chưa tồn tại -> chạy toàn bộ *.sql
#      trong /docker-entrypoint-initdb.d theo thứ tự tên file
#   4. Giữ container sống cùng tiến trình sqlservr
# =====================================================================================
set -euo pipefail

SA_PASSWORD="${MSSQL_SA_PASSWORD:-}"
INIT_DIR="/docker-entrypoint-initdb.d"

if [ -z "$SA_PASSWORD" ]; then
    echo "[entrypoint] LỖI: chưa đặt biến môi trường MSSQL_SA_PASSWORD." >&2
    exit 1
fi

# --- 1. Tìm sqlcmd (image 2022 mới dùng mssql-tools18) -------------------------------
SQLCMD=""
TRUST_FLAG=""
for candidate in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
    if [ -x "$candidate" ]; then
        SQLCMD="$candidate"
        break
    fi
done
if [ -z "$SQLCMD" ] && command -v sqlcmd >/dev/null 2>&1; then
    SQLCMD="$(command -v sqlcmd)"
fi

case "$SQLCMD" in
    *mssql-tools18*) TRUST_FLAG="-C" ;;
    *)               TRUST_FLAG="" ;;
esac

if [ -z "$SQLCMD" ]; then
    echo "[entrypoint] CẢNH BÁO: không tìm thấy sqlcmd -> bỏ qua bước init schema." >&2
fi

run_sql() {
    # $1 = file .sql
    "$SQLCMD" -S localhost -U sa -P "$SA_PASSWORD" $TRUST_FLAG -b -I -f 65001 -i "$1"
}

# --- 2. Khởi động SQL Server ở nền ---------------------------------------------------
echo "[entrypoint] Khởi động SQL Server..."
/opt/mssql/bin/sqlservr &
SQLSERVR_PID=$!

cleanup() {
    echo "[entrypoint] Dừng SQL Server..."
    kill -TERM "$SQLSERVR_PID" 2>/dev/null || true
}
trap cleanup TERM INT

# --- 3. Chờ server sẵn sàng ----------------------------------------------------------
if [ -n "$SQLCMD" ]; then
    READY=0
    for _ in $(seq 1 90); do
        if "$SQLCMD" -S localhost -U sa -P "$SA_PASSWORD" $TRUST_FLAG -Q "SELECT 1" >/dev/null 2>&1; then
            READY=1
            break
        fi
        sleep 2
    done

    if [ "$READY" -ne 1 ]; then
        echo "[entrypoint] CẢNH BÁO: SQL Server chưa sẵn sàng sau 180s, bỏ qua init." >&2
    else
        echo "[entrypoint] SQL Server đã sẵn sàng."

        # --- 4. Init schema một lần duy nhất --------------------------------------
        DB_EXISTS="$("$SQLCMD" -S localhost -U sa -P "$SA_PASSWORD" $TRUST_FLAG -h -1 -W \
                    -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('RestaurantManagement') IS NULL THEN '0' ELSE '1' END;" \
                    | tr -d '[:space:]')"

        if [ "$DB_EXISTS" = "0" ]; then
            if [ -d "$INIT_DIR" ] && compgen -G "$INIT_DIR/*.sql" > /dev/null; then
                for f in $(ls -1 "$INIT_DIR"/*.sql | sort); do
                    echo "[entrypoint] Đang chạy $(basename "$f")..."
                    run_sql "$f"
                    echo "[entrypoint] Xong $(basename "$f")."
                done
                echo "[entrypoint] Khởi tạo database hoàn tất."
            else
                echo "[entrypoint] Không có script .sql trong $INIT_DIR."
            fi
        else
            echo "[entrypoint] Database RestaurantManagement đã tồn tại -> bỏ qua init (tránh ghi đè dữ liệu)."
        fi
    fi
fi

# --- 5. Giữ container sống -----------------------------------------------------------
wait "$SQLSERVR_PID"
