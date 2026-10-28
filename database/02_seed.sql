/* =====================================================================================
   Restaurant Management System — SQL Server 2022
   File    : 02_seed.sql
   Mục đích: Dữ liệu mẫu tối thiểu để chạy và demo (idempotent — chạy lại không nhân bản)
   Chạy    : sqlcmd -S localhost,1433 -U sa -P <password> -C -b -i 02_seed.sql
   Tài khoản mẫu:
       admin    / Admin@123   (vai trò ADMIN, không gắn nhân viên)
       manager  / Admin@123   (vai trò MANAGER, gắn nhân viên NV001)
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE [RestaurantManagement];
GO



/* ---------- 1. Vai trò ------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE [Code] = 'ADMIN')
    INSERT INTO dbo.[Role] ([Code], [Name], [Description], [IsSystem])
    VALUES ('ADMIN',   N'Admin',    N'Quản trị hệ thống', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE [Code] = 'OWNER')
    INSERT INTO dbo.[Role] ([Code], [Name], [Description], [IsSystem])
    VALUES ('OWNER',   N'Chủ quán', N'Toàn quyền kinh doanh và cấu hình', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE [Code] = 'MANAGER')
    INSERT INTO dbo.[Role] ([Code], [Name], [Description], [IsSystem])
    VALUES ('MANAGER', N'Quản lý',  N'Điều hành, duyệt chấm công, báo cáo vận hành', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE [Code] = 'CASHIER')
    INSERT INTO dbo.[Role] ([Code], [Name], [Description], [IsSystem])
    VALUES ('CASHIER', N'Thu ngân', N'Thanh toán, in hóa đơn', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE [Code] = 'WAITER')
    INSERT INTO dbo.[Role] ([Code], [Name], [Description], [IsSystem])
    VALUES ('WAITER',  N'Phục vụ',  N'Chọn bàn, gọi món, phục vụ', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE [Code] = 'KITCHEN')
    INSERT INTO dbo.[Role] ([Code], [Name], [Description], [IsSystem])
    VALUES ('KITCHEN', N'Bếp',      N'Nhận món và cập nhật trạng thái', 0);
GO

/* ---------- 2. Vị trí công việc ---------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Position] WHERE [Code] = 'WAITER')
    INSERT INTO dbo.[Position] ([Code], [Name], [DefaultRoleId], [DefaultShiftRate])
    SELECT 'WAITER', N'Nhân viên phục vụ', [Id], 200000 FROM dbo.[Role] WHERE [Code] = 'WAITER';

IF NOT EXISTS (SELECT 1 FROM dbo.[Position] WHERE [Code] = 'BARTENDER')
    INSERT INTO dbo.[Position] ([Code], [Name], [DefaultRoleId], [DefaultShiftRate])
    SELECT 'BARTENDER', N'Nhân viên pha chế', [Id], 220000 FROM dbo.[Role] WHERE [Code] = 'KITCHEN';

IF NOT EXISTS (SELECT 1 FROM dbo.[Position] WHERE [Code] = 'MANAGER')
    INSERT INTO dbo.[Position] ([Code], [Name], [DefaultRoleId], [DefaultShiftRate])
    SELECT 'MANAGER', N'Nhân viên quản lý', [Id], 300000 FROM dbo.[Role] WHERE [Code] = 'MANAGER';

IF NOT EXISTS (SELECT 1 FROM dbo.[Position] WHERE [Code] = 'RECEPTION')
    INSERT INTO dbo.[Position] ([Code], [Name], [DefaultRoleId], [DefaultShiftRate])
    SELECT 'RECEPTION', N'Nhân viên tiếp tân', [Id], 190000 FROM dbo.[Role] WHERE [Code] = 'CASHIER';
GO

/* ---------- 3. Trình độ vi tính ---------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Skill] WHERE [Name] = N'Word')
    INSERT INTO dbo.[Skill] ([Name], [DisplayOrder]) VALUES (N'Word', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[Skill] WHERE [Name] = N'Excel')
    INSERT INTO dbo.[Skill] ([Name], [DisplayOrder]) VALUES (N'Excel', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[Skill] WHERE [Name] = N'Cài phần mềm')
    INSERT INTO dbo.[Skill] ([Name], [DisplayOrder]) VALUES (N'Cài phần mềm', 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[Skill] WHERE [Name] = N'Khác')
    INSERT INTO dbo.[Skill] ([Name], [DisplayOrder]) VALUES (N'Khác', 4);
GO

/* ---------- 4. Ca làm việc (2 ca theo yêu cầu chủ quán) ---------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Shift] WHERE [Name] = N'Ca sáng')
    INSERT INTO dbo.[Shift] ([Name], [StartTime], [EndTime]) VALUES (N'Ca sáng', '09:00', '16:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[Shift] WHERE [Name] = N'Ca chiều')
    INSERT INTO dbo.[Shift] ([Name], [StartTime], [EndTime]) VALUES (N'Ca chiều', '16:00', '22:00');
GO

/* ---------- 5. Nhân viên ----------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Employee] WHERE [EmployeeCode] = 'NV001')
    INSERT INTO dbo.[Employee] ([EmployeeCode], [FullName], [Gender], [DateOfBirth], [PositionId],
                                [Phone], [Address], [IdentityNumber], [StartDate], [Status], [Note])
    SELECT 'NV001', N'Nguyễn Văn An', 1, '1988-04-12', p.[Id],
           '0901234567', N'12 Nguyễn Trãi, Quận 1, TP.HCM', '079088001234', '2024-01-05', 0, N'Nhân viên quản lý'
    FROM dbo.[Position] p WHERE p.[Code] = 'MANAGER';

IF NOT EXISTS (SELECT 1 FROM dbo.[Employee] WHERE [EmployeeCode] = 'NV002')
    INSERT INTO dbo.[Employee] ([EmployeeCode], [FullName], [Gender], [DateOfBirth], [PositionId],
                                [Phone], [Address], [IdentityNumber], [StartDate], [Status], [Note])
    SELECT 'NV002', N'Trần Thị Bình', 0, '1999-09-02', p.[Id],
           '0912345678', N'45 Lê Lợi, Quận 1, TP.HCM', '079199002345', '2024-03-01', 0, N'Phục vụ ca sáng'
    FROM dbo.[Position] p WHERE p.[Code] = 'WAITER';

IF NOT EXISTS (SELECT 1 FROM dbo.[Employee] WHERE [EmployeeCode] = 'NV003')
    INSERT INTO dbo.[Employee] ([EmployeeCode], [FullName], [Gender], [DateOfBirth], [PositionId],
                                [Phone], [Address], [IdentityNumber], [StartDate], [Status], [Note])
    SELECT 'NV003', N'Lê Hoàng Cường', 1, '2000-12-21', p.[Id],
           '0923456789', N'78 Pasteur, Quận 3, TP.HCM', '079200003456', '2024-06-15', 0, N'Phục vụ ca chiều'
    FROM dbo.[Position] p WHERE p.[Code] = 'WAITER';

IF NOT EXISTS (SELECT 1 FROM dbo.[Employee] WHERE [EmployeeCode] = 'NV004')
    INSERT INTO dbo.[Employee] ([EmployeeCode], [FullName], [Gender], [DateOfBirth], [PositionId],
                                [Phone], [Address], [IdentityNumber], [StartDate], [Status], [Note])
    SELECT 'NV004', N'Phạm Minh Dũng', 1, '1997-07-07', p.[Id],
           '0934567890', N'23 Võ Văn Tần, Quận 3, TP.HCM', '079197004567', '2024-08-20', 0, N'Pha chế'
    FROM dbo.[Position] p WHERE p.[Code] = 'BARTENDER';
GO

/* ---------- 6. Tài khoản ----------------------------------------------------------- */
/* PasswordHash dưới đây = PBKDF2-HMAC-SHA512, 210.000 vòng, salt 16 byte (ASP.NET Identity V3) cho mật khẩu Admin@123 */
IF NOT EXISTS (SELECT 1 FROM dbo.[AppUser] WHERE [Username] = 'admin')
    INSERT INTO dbo.[AppUser] ([Username], [PasswordHash], [RoleId], [EmployeeId], [IsActive])
    SELECT 'admin',
           'AQAAAAIAAzRQAAAAEAECAwQFBgcICQoLDA0ODxAd8Zhog4/42J1y6UeLEZ9/40wCMIySmGHPP0ZWCxXXrg==',
           r.[Id], NULL, 1
    FROM dbo.[Role] r WHERE r.[Code] = 'ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.[AppUser] WHERE [Username] = 'manager')
    INSERT INTO dbo.[AppUser] ([Username], [PasswordHash], [RoleId], [EmployeeId], [IsActive])
    SELECT 'manager',
           'AQAAAAIAAzRQAAAAEAECAwQFBgcICQoLDA0ODxAd8Zhog4/42J1y6UeLEZ9/40wCMIySmGHPP0ZWCxXXrg==',
           r.[Id], e.[Id], 1
    FROM dbo.[Role] r
    CROSS JOIN dbo.[Employee] e
    WHERE r.[Code] = 'MANAGER' AND e.[EmployeeCode] = 'NV001';
GO

/* ---------- 7. Trình độ của nhân viên ---------------------------------------------- */
INSERT INTO dbo.[EmployeeSkill] ([EmployeeId], [SkillId])
SELECT e.[Id], s.[Id]
FROM dbo.[Employee] e
CROSS JOIN dbo.[Skill] s
WHERE ((e.[EmployeeCode] = 'NV001' AND s.[Name] IN (N'Word', N'Excel'))
    OR (e.[EmployeeCode] = 'NV002' AND s.[Name] IN (N'Word'))
    OR (e.[EmployeeCode] = 'NV003' AND s.[Name] IN (N'Excel'))
    OR (e.[EmployeeCode] = 'NV004' AND s.[Name] IN (N'Excel', N'Cài phần mềm')))
  AND NOT EXISTS (
        SELECT 1 FROM dbo.[EmployeeSkill] es
        WHERE es.[EmployeeId] = e.[Id] AND es.[SkillId] = s.[Id]);
GO

/* ---------- 8. Ca mà nhân viên có thể làm ------------------------------------------ */
INSERT INTO dbo.[EmployeeShift] ([EmployeeId], [ShiftId])
SELECT e.[Id], s.[Id]
FROM dbo.[Employee] e
CROSS JOIN dbo.[Shift] s
WHERE NOT EXISTS (
        SELECT 1 FROM dbo.[EmployeeShift] es
        WHERE es.[EmployeeId] = e.[Id] AND es.[ShiftId] = s.[Id]);
GO

/* ---------- 9. Dụng cụ / trang phục ----------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[UniformItem] WHERE [Code] = 'U001')
    INSERT INTO dbo.[UniformItem] ([Code], [Name], [Unit]) VALUES ('U001', N'Áo sơ mi đồng phục', N'cái');
IF NOT EXISTS (SELECT 1 FROM dbo.[UniformItem] WHERE [Code] = 'U002')
    INSERT INTO dbo.[UniformItem] ([Code], [Name], [Unit]) VALUES ('U002', N'Tạp dề', N'cái');
IF NOT EXISTS (SELECT 1 FROM dbo.[UniformItem] WHERE [Code] = 'U003')
    INSERT INTO dbo.[UniformItem] ([Code], [Name], [Unit]) VALUES ('U003', N'Khăn bàn', N'cái');
GO

INSERT INTO dbo.[UniformIssue] ([EmployeeId], [UniformItemId], [Quantity], [IssuedAt], [Status])
SELECT e.[Id], u.[Id], 2, e.[StartDate], 0
FROM dbo.[Employee] e
CROSS JOIN dbo.[UniformItem] u
WHERE u.[Code] = 'U001'
  AND NOT EXISTS (SELECT 1 FROM dbo.[UniformIssue] ui WHERE ui.[EmployeeId] = e.[Id] AND ui.[UniformItemId] = u.[Id]);
GO

/* ---------- 10. Khu vực & bàn ----------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Area] WHERE [Name] = N'Tầng 1')
    INSERT INTO dbo.[Area] ([Name], [DisplayOrder]) VALUES (N'Tầng 1', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[Area] WHERE [Name] = N'Tầng 2')
    INSERT INTO dbo.[Area] ([Name], [DisplayOrder]) VALUES (N'Tầng 2', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[Area] WHERE [Name] = N'Sân vườn')
    INSERT INTO dbo.[Area] ([Name], [DisplayOrder]) VALUES (N'Sân vườn', 3);
GO

;WITH TableSeed AS (
    SELECT * FROM (VALUES
        ('B01', 4, 1), ('B02', 4, 1), ('B03', 2, 1), ('B04', 6, 1),
        ('B05', 4, 2), ('B06', 4, 2), ('B07', 8, 2),
        ('B08', 4, 3), ('B09', 6, 3), ('B10', 10, 3)
    ) AS v(TableNumber, MaxSeats, AreaOrder)
)
INSERT INTO dbo.[DiningTable] ([TableNumber], [AreaId], [MaxSeats])
SELECT t.TableNumber, a.[Id], t.MaxSeats
FROM TableSeed t
JOIN dbo.[Area] a ON a.[DisplayOrder] = t.AreaOrder
WHERE NOT EXISTS (SELECT 1 FROM dbo.[DiningTable] dt WHERE dt.[TableNumber] = t.TableNumber);
GO

/* ---------- 11. Nhóm món & món ----------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[Category] WHERE [Code] = 'KHAIVI')
    INSERT INTO dbo.[Category] ([Code], [Name], [DisplayOrder]) VALUES ('KHAIVI', N'Khai vị', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[Category] WHERE [Code] = 'CHINH')
    INSERT INTO dbo.[Category] ([Code], [Name], [DisplayOrder]) VALUES ('CHINH', N'Món chính', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[Category] WHERE [Code] = 'TRANGMIENG')
    INSERT INTO dbo.[Category] ([Code], [Name], [DisplayOrder]) VALUES ('TRANGMIENG', N'Tráng miệng', 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[Category] WHERE [Code] = 'DOUONG')
    INSERT INTO dbo.[Category] ([Code], [Name], [DisplayOrder]) VALUES ('DOUONG', N'Đồ uống', 4);
GO

INSERT INTO dbo.[MenuItem] ([Code], [Name], [CategoryId], [Price], [Unit], [Description])
SELECT v.[Code], v.[Name], c.[Id], v.[Price], v.[Unit], v.[Description]
FROM (VALUES
    ('M001', N'Gỏi cuốn tôm thịt',  'KHAIVI',     45000.00, N'phần', N'2 cuốn, kèm nước chấm'),
    ('M002', N'Chả giò chiên',      'KHAIVI',     55000.00, N'phần', NULL),
    ('M003', N'Cơm tấm sườn bì',    'CHINH',      65000.00, N'phần', NULL),
    ('M004', N'Phở bò tái',         'CHINH',      70000.00, N'tô',   NULL),
    ('M005', N'Bún chả Hà Nội',     'CHINH',      75000.00, N'phần', NULL),
    ('M006', N'Cá hồi nướng',       'CHINH',     180000.00, N'phần', N'Cá hồi Na Uy'),
    ('M007', N'Lẩu thái hải sản',   'CHINH',     320000.00, N'nồi',  N'Cho 2-3 người'),
    ('M008', N'Chè ba màu',         'TRANGMIENG', 30000.00, N'ly',   NULL),
    ('M009', N'Bánh flan',          'TRANGMIENG', 25000.00, N'cái',  NULL),
    ('M010', N'Cà phê sữa đá',      'DOUONG',     35000.00, N'ly',   NULL),
    ('M011', N'Trà đào cam sả',     'DOUONG',     45000.00, N'ly',   NULL),
    ('M012', N'Nước cam tươi',      'DOUONG',     40000.00, N'ly',   NULL),
    ('M013', N'Bia Tiger',          'DOUONG',     25000.00, N'chai', NULL),
    ('M014', N'Pepsi',              'DOUONG',     20000.00, N'lon',  NULL)
) AS v([Code], [Name], [CategoryCode], [Price], [Unit], [Description])
JOIN dbo.[Category] c ON c.[Code] = v.[CategoryCode]
WHERE NOT EXISTS (SELECT 1 FROM dbo.[MenuItem] m WHERE m.[Code] = v.[Code]);
GO

/* ---------- 12. Tham số hệ thống --------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[SystemParameter] WHERE [ParamKey] = 'ShiftsPerDay')
    INSERT INTO dbo.[SystemParameter] ([ParamKey], [ParamValue], [DataType], [Description])
    VALUES ('ShiftsPerDay', '2', 'int', N'Số ca mặc định trong ngày');

IF NOT EXISTS (SELECT 1 FROM dbo.[SystemParameter] WHERE [ParamKey] = 'DefaultShiftRate')
    INSERT INTO dbo.[SystemParameter] ([ParamKey], [ParamValue], [DataType], [Description])
    VALUES ('DefaultShiftRate', '200000', 'decimal', N'Đơn giá ca mặc định (VND)');

IF NOT EXISTS (SELECT 1 FROM dbo.[SystemParameter] WHERE [ParamKey] = 'VatPercent')
    INSERT INTO dbo.[SystemParameter] ([ParamKey], [ParamValue], [DataType], [Description])
    VALUES ('VatPercent', '0', 'decimal', N'Thuế VAT áp dụng lên hóa đơn (%)');

IF NOT EXISTS (SELECT 1 FROM dbo.[SystemParameter] WHERE [ParamKey] = 'Currency')
    INSERT INTO dbo.[SystemParameter] ([ParamKey], [ParamValue], [DataType], [Description])
    VALUES ('Currency', 'VND', 'string', N'Đơn vị tiền tệ');

IF NOT EXISTS (SELECT 1 FROM dbo.[SystemParameter] WHERE [ParamKey] = 'ReportShiftAnchor')
    INSERT INTO dbo.[SystemParameter] ([ParamKey], [ParamValue], [DataType], [Description])
    VALUES ('ReportShiftAnchor', 'OpenedAt', 'string', N'Mốc gán ca cho báo cáo: OpenedAt hoặc ClosedAt');

IF NOT EXISTS (SELECT 1 FROM dbo.[SystemParameter] WHERE [ParamKey] = 'RestaurantName')
    INSERT INTO dbo.[SystemParameter] ([ParamKey], [ParamValue], [DataType], [Description])
    VALUES ('RestaurantName', N'Quán Ăn Demo', 'string', N'Tên quán in trên hóa đơn');
GO

/* ---------- 13. Lịch làm việc mẫu (hôm nay & ngày mai) ----------------------------- */
DECLARE @today date = CAST(SYSUTCDATETIME() AS date);

INSERT INTO dbo.[Schedule] ([EmployeeId], [ShiftId], [WorkDate], [Status], [ApprovedBy])
SELECT e.[Id], s.[Id], d.[WorkDate], 1, m.[Id]
FROM (VALUES
    (CAST(@today AS date),                    N'Ca sáng',  'NV001'),
    (CAST(@today AS date),                    N'Ca sáng',  'NV002'),
    (CAST(@today AS date),                    N'Ca chiều', 'NV003'),
    (CAST(@today AS date),                    N'Ca chiều', 'NV004'),
    (CAST(DATEADD(day, 1, @today) AS date),   N'Ca sáng',  'NV002'),
    (CAST(DATEADD(day, 1, @today) AS date),   N'Ca chiều', 'NV003')
) AS d([WorkDate], [ShiftName], [EmployeeCode])
JOIN dbo.[Shift] s    ON s.[Name] = d.[ShiftName]
JOIN dbo.[Employee] e ON e.[EmployeeCode] = d.[EmployeeCode]
JOIN dbo.[Employee] m ON m.[EmployeeCode] = 'NV001'
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.[Schedule] sc
    WHERE sc.[EmployeeId] = e.[Id] AND sc.[ShiftId] = s.[Id] AND sc.[WorkDate] = d.[WorkDate]);
GO

PRINT N'02_seed.sql: hoàn tất nạp dữ liệu mẫu.';
GO
