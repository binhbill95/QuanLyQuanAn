# =====================================================================================
# SQL Server 2022 + tự động chạy script trong /docker-entrypoint-initdb.d
# Build: docker compose build
# =====================================================================================
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

USER mssql

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
