BACKUP SQL SERVER - RESTAURANT MANAGEMENT

Thoi diem tao: 2026-10-28
Database: RestaurantManagement
Nen tang: Microsoft SQL Server 2022

Noi dung thu muc:
- RestaurantManagement.bak: ban backup database tao truc tiep tu SQL Server trong Docker.
- RestaurantManagement_full.sql: script tu tao lai schema va INSERT du lieu hien co khi xuat.
- 01_schema.sql: tao database, bang, khoa va rang buoc.
- 02_seed.sql: nap du lieu mau.
- 03_views.sql: tao cac view.
- 04_audit.sql: tao trigger audit.
- 05_rules.sql: tao trigger quy tac nghiep vu.

Mo/xem database:
1. Dung SQL Server Management Studio (SSMS) ket noi den mot SQL Server.
2. Chuot phai Databases > Restore Database...
3. Chon Device, Browse den RestaurantManagement.bak va thuc hien Restore.

Neu muon xem cau truc bang script, mo 01_schema.sql bang VS Code hoac SSMS.
Khong can chay lai cac script tren database da duoc restore tu file .bak.

Luu y: file full SQL chua du lieu nhan vien va PasswordHash cua tai khoan ung dung.
Hay bao ve file nhu du lieu rieng tu; PasswordHash khong phai mat khau goc.

Khoi phuc bang script full SQL tren SQL Server chua co database nay:
sqlcmd -S localhost,1433 -U sa -C -b -I -f 65001 -i RestaurantManagement_full.sql

Script da duoc thu restore vao database tam: 25 bang, 10 view, 7 trigger, 97 dong du lieu.
