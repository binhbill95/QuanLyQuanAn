/* =====================================================================================
   Restaurant Management System — SQL Server 2022
   File    : 01_schema.sql
   Mục đích: Tạo database, 25 bảng, PK/FK, CHECK, UNIQUE, INDEX theo docs/11_DatabaseSchemaDesign.md
   Chạy    : sqlcmd -S localhost,1433 -U sa -P <password> -C -b -i 01_schema.sql
   Lưu ý    : Script idempotent ở mức database; các bảng dùng IF OBJECT_ID(...) IS NULL
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ---------- 0. Database ------------------------------------------------------------ */
IF DB_ID(N'RestaurantManagement') IS NULL
BEGIN
    CREATE DATABASE [RestaurantManagement] COLLATE Vietnamese_CI_AS;
END
GO

IF SERVERPROPERTY('ProductMajorVersion') >= 15
    ALTER DATABASE [RestaurantManagement] SET RECOVERY SIMPLE;
GO

USE [RestaurantManagement];
GO

/* =====================================================================================
   1. NHÓM BẢO MẬT
   ===================================================================================== */

IF OBJECT_ID(N'dbo.Role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Role]
    (
        [Id]          int            IDENTITY(1,1) NOT NULL,
        [Code]        varchar(20)    NOT NULL,
        [Name]        nvarchar(50)   NOT NULL,
        [Description] nvarchar(255)  NULL,
        [IsSystem]    bit            NOT NULL CONSTRAINT DF_Role_IsSystem DEFAULT (0),

        [CreatedAt]   datetime2(0)   NOT NULL CONSTRAINT DF_Role_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]   int            NULL,
        [UpdatedAt]   datetime2(0)   NULL,
        [UpdatedBy]   int            NULL,
        [IsDeleted]   bit            NOT NULL CONSTRAINT DF_Role_IsDeleted DEFAULT (0),
        [DeletedAt]   datetime2(0)   NULL,
        [RowVersion]  rowversion     NOT NULL,

        CONSTRAINT PK_Role PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Role_Code UNIQUE ([Code]),
        CONSTRAINT CK_Role_Code_NotEmpty CHECK (LEN(LTRIM(RTRIM([Code]))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Permission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Permission]
    (
        [Id]         int            IDENTITY(1,1) NOT NULL,
        [Code]       varchar(100)   NOT NULL,
        [Name]       nvarchar(150)  NOT NULL,
        [ModuleName] nvarchar(50)   NULL,

        [CreatedAt]  datetime2(0)   NOT NULL CONSTRAINT DF_Permission_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int            NULL,
        [UpdatedAt]  datetime2(0)   NULL,
        [UpdatedBy]  int            NULL,
        [IsDeleted]  bit            NOT NULL CONSTRAINT DF_Permission_IsDeleted DEFAULT (0),
        [DeletedAt]  datetime2(0)   NULL,
        [RowVersion] rowversion     NOT NULL,

        CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Permission_Code UNIQUE ([Code])
    );
END
GO

IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[RolePermission]
    (
        [RoleId]       int          NOT NULL,
        [PermissionId] int          NOT NULL,
        [CreatedAt]    datetime2(0) NOT NULL CONSTRAINT DF_RolePermission_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]    int          NULL,

        CONSTRAINT PK_RolePermission PRIMARY KEY CLUSTERED ([RoleId], [PermissionId]),
        CONSTRAINT FK_RolePermission_Role FOREIGN KEY ([RoleId]) REFERENCES dbo.[Role] ([Id]),
        CONSTRAINT FK_RolePermission_Permission FOREIGN KEY ([PermissionId]) REFERENCES dbo.[Permission] ([Id])
    );
END
GO

/* =====================================================================================
   2. NHÓM NHÂN SỰ
   ===================================================================================== */

IF OBJECT_ID(N'dbo.Position', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Position]
    (
        [Id]                int            IDENTITY(1,1) NOT NULL,
        [Code]              varchar(20)    NOT NULL,
        [Name]              nvarchar(50)   NOT NULL,
        [DefaultRoleId]     int            NULL,
        [DefaultShiftRate]  decimal(18,2)  NOT NULL CONSTRAINT DF_Position_DefaultShiftRate DEFAULT (0),
        [IsActive]          bit            NOT NULL CONSTRAINT DF_Position_IsActive DEFAULT (1),

        [CreatedAt]         datetime2(0)   NOT NULL CONSTRAINT DF_Position_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]         int            NULL,
        [UpdatedAt]         datetime2(0)   NULL,
        [UpdatedBy]         int            NULL,
        [IsDeleted]         bit            NOT NULL CONSTRAINT DF_Position_IsDeleted DEFAULT (0),
        [DeletedAt]         datetime2(0)   NULL,
        [RowVersion]        rowversion     NOT NULL,

        CONSTRAINT PK_Position PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Position_Code UNIQUE ([Code]),
        CONSTRAINT UQ_Position_Name UNIQUE ([Name]),
        CONSTRAINT CK_Position_Rate CHECK ([DefaultShiftRate] >= 0),
        CONSTRAINT FK_Position_Role FOREIGN KEY ([DefaultRoleId]) REFERENCES dbo.[Role] ([Id]) ON DELETE SET NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Employee]
    (
        [Id]             int            IDENTITY(1,1) NOT NULL,
        [EmployeeCode]   varchar(20)    NOT NULL,
        [FullName]       nvarchar(100)  NOT NULL,
        [Gender]         tinyint        NULL,
        [DateOfBirth]    date           NULL,
        [PositionId]     int            NOT NULL,
        [Phone]          nvarchar(20)   NULL,
        [Address]        nvarchar(255)  NULL,
        [IdentityNumber] varchar(20)    NOT NULL,
        [StartDate]      date           NOT NULL,
        [EndDate]        date           NULL,
        [ShiftRate]      decimal(18,2)  NULL,
        [Status]         tinyint        NOT NULL CONSTRAINT DF_Employee_Status DEFAULT (0),
        [Note]           nvarchar(255)  NULL,

        [CreatedAt]      datetime2(0)   NOT NULL CONSTRAINT DF_Employee_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]      int            NULL,
        [UpdatedAt]      datetime2(0)   NULL,
        [UpdatedBy]      int            NULL,
        [IsDeleted]      bit            NOT NULL CONSTRAINT DF_Employee_IsDeleted DEFAULT (0),
        [DeletedAt]      datetime2(0)   NULL,
        [RowVersion]     rowversion     NOT NULL,

        CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Employee_Code UNIQUE ([EmployeeCode]),
        CONSTRAINT UQ_Employee_IdentityNumber UNIQUE ([IdentityNumber]),
        CONSTRAINT CK_Employee_Gender CHECK ([Gender] IS NULL OR [Gender] IN (0, 1, 2)),
        CONSTRAINT CK_Employee_Status CHECK ([Status] IN (0, 1, 2)),
        CONSTRAINT CK_Employee_ShiftRate CHECK ([ShiftRate] IS NULL OR [ShiftRate] >= 0),
        CONSTRAINT CK_Employee_EndDate CHECK ([EndDate] IS NULL OR [EndDate] >= [StartDate]),
        CONSTRAINT CK_Employee_Resigned CHECK ([Status] <> 2 OR [EndDate] IS NOT NULL),
        CONSTRAINT FK_Employee_Position FOREIGN KEY ([PositionId]) REFERENCES dbo.[Position] ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.AppUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[AppUser]
    (
        [Id]           int            IDENTITY(1,1) NOT NULL,
        [Username]     nvarchar(50)   NOT NULL,
        [PasswordHash] nvarchar(255)  NOT NULL,
        [RoleId]       int            NOT NULL,
        [EmployeeId]   int            NULL,
        [IsActive]     bit            NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1),
        [LastLoginAt]  datetime2(0)   NULL,

        [CreatedAt]    datetime2(0)   NOT NULL CONSTRAINT DF_AppUser_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]    int            NULL,
        [UpdatedAt]    datetime2(0)   NULL,
        [UpdatedBy]    int            NULL,
        [IsDeleted]    bit            NOT NULL CONSTRAINT DF_AppUser_IsDeleted DEFAULT (0),
        [DeletedAt]    datetime2(0)   NULL,
        [RowVersion]   rowversion     NOT NULL,

        CONSTRAINT PK_AppUser PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_AppUser_Username UNIQUE ([Username]),
        CONSTRAINT FK_AppUser_Role FOREIGN KEY ([RoleId]) REFERENCES dbo.[Role] ([Id]),
        CONSTRAINT FK_AppUser_Employee FOREIGN KEY ([EmployeeId]) REFERENCES dbo.[Employee] ([Id]) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX UX_AppUser_EmployeeId ON dbo.[AppUser] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.Skill', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Skill]
    (
        [Id]           int           IDENTITY(1,1) NOT NULL,
        [Name]         nvarchar(50)  NOT NULL,
        [DisplayOrder] int           NOT NULL CONSTRAINT DF_Skill_DisplayOrder DEFAULT (0),
        [IsActive]     bit           NOT NULL CONSTRAINT DF_Skill_IsActive DEFAULT (1),

        [CreatedAt]    datetime2(0)  NOT NULL CONSTRAINT DF_Skill_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]    int           NULL,
        [UpdatedAt]    datetime2(0)  NULL,
        [UpdatedBy]    int           NULL,
        [IsDeleted]    bit           NOT NULL CONSTRAINT DF_Skill_IsDeleted DEFAULT (0),
        [DeletedAt]    datetime2(0)  NULL,
        [RowVersion]   rowversion    NOT NULL,

        CONSTRAINT PK_Skill PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Skill_Name UNIQUE ([Name])
    );
END
GO

IF OBJECT_ID(N'dbo.EmployeeSkill', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[EmployeeSkill]
    (
        [EmployeeId] int          NOT NULL,
        [SkillId]    int          NOT NULL,
        [CreatedAt]  datetime2(0) NOT NULL CONSTRAINT DF_EmployeeSkill_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int          NULL,

        CONSTRAINT PK_EmployeeSkill PRIMARY KEY CLUSTERED ([EmployeeId], [SkillId]),
        CONSTRAINT FK_EmployeeSkill_Employee FOREIGN KEY ([EmployeeId]) REFERENCES dbo.[Employee] ([Id]),
        CONSTRAINT FK_EmployeeSkill_Skill FOREIGN KEY ([SkillId]) REFERENCES dbo.[Skill] ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.Shift', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Shift]
    (
        [Id]         int           IDENTITY(1,1) NOT NULL,
        [Name]       nvarchar(50)  NOT NULL,
        [StartTime]  time(0)       NOT NULL,
        [EndTime]    time(0)       NOT NULL,
        [IsActive]   bit           NOT NULL CONSTRAINT DF_Shift_IsActive DEFAULT (1),

        [CreatedAt]  datetime2(0)  NOT NULL CONSTRAINT DF_Shift_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int           NULL,
        [UpdatedAt]  datetime2(0)  NULL,
        [UpdatedBy]  int           NULL,
        [IsDeleted]  bit           NOT NULL CONSTRAINT DF_Shift_IsDeleted DEFAULT (0),
        [DeletedAt]  datetime2(0)  NULL,
        [RowVersion] rowversion    NOT NULL,

        CONSTRAINT PK_Shift PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Shift_Name UNIQUE ([Name]),
        CONSTRAINT CK_Shift_Time CHECK ([StartTime] < [EndTime])
    );
END
GO

IF OBJECT_ID(N'dbo.EmployeeShift', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[EmployeeShift]
    (
        [EmployeeId] int          NOT NULL,
        [ShiftId]    int          NOT NULL,
        [CreatedAt]  datetime2(0) NOT NULL CONSTRAINT DF_EmployeeShift_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int          NULL,

        CONSTRAINT PK_EmployeeShift PRIMARY KEY CLUSTERED ([EmployeeId], [ShiftId]),
        CONSTRAINT FK_EmployeeShift_Employee FOREIGN KEY ([EmployeeId]) REFERENCES dbo.[Employee] ([Id]),
        CONSTRAINT FK_EmployeeShift_Shift FOREIGN KEY ([ShiftId]) REFERENCES dbo.[Shift] ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.Schedule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Schedule]
    (
        [Id]         int           IDENTITY(1,1) NOT NULL,
        [EmployeeId] int           NOT NULL,
        [ShiftId]    int           NOT NULL,
        [WorkDate]   date          NOT NULL,
        [Status]     tinyint       NOT NULL CONSTRAINT DF_Schedule_Status DEFAULT (0),
        [ApprovedBy] int           NULL,
        [ApprovedAt] datetime2(0)  NULL,
        [Note]       nvarchar(255) NULL,

        [CreatedAt]  datetime2(0)  NOT NULL CONSTRAINT DF_Schedule_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int           NULL,
        [UpdatedAt]  datetime2(0)  NULL,
        [UpdatedBy]  int           NULL,
        [IsDeleted]  bit           NOT NULL CONSTRAINT DF_Schedule_IsDeleted DEFAULT (0),
        [DeletedAt]  datetime2(0)  NULL,
        [RowVersion] rowversion    NOT NULL,

        CONSTRAINT PK_Schedule PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Schedule_Employee_Shift_Date UNIQUE ([EmployeeId], [ShiftId], [WorkDate]),
        CONSTRAINT CK_Schedule_Status CHECK ([Status] IN (0, 1, 2)),
        CONSTRAINT FK_Schedule_Employee FOREIGN KEY ([EmployeeId]) REFERENCES dbo.[Employee] ([Id]),
        CONSTRAINT FK_Schedule_Shift FOREIGN KEY ([ShiftId]) REFERENCES dbo.[Shift] ([Id]),
        CONSTRAINT FK_Schedule_ApprovedBy FOREIGN KEY ([ApprovedBy]) REFERENCES dbo.[Employee] ([Id])
    );

    CREATE INDEX IX_Schedule_WorkDate_Shift ON dbo.[Schedule] ([WorkDate], [ShiftId]);
    CREATE INDEX IX_Schedule_Employee_WorkDate ON dbo.[Schedule] ([EmployeeId], [WorkDate]);
END
GO

IF OBJECT_ID(N'dbo.Attendance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Attendance]
    (
        [Id]         int           IDENTITY(1,1) NOT NULL,
        [ScheduleId] int           NOT NULL,
        [CheckInAt]  datetime2(0)  NOT NULL,
        [CheckOutAt] datetime2(0)  NULL,
        [Status]     tinyint       NOT NULL CONSTRAINT DF_Attendance_Status DEFAULT (0),
        [ApprovedBy] int           NULL,
        [ApprovedAt] datetime2(0)  NULL,

        [CreatedAt]  datetime2(0)  NOT NULL CONSTRAINT DF_Attendance_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int           NULL,
        [UpdatedAt]  datetime2(0)  NULL,
        [UpdatedBy]  int           NULL,
        [IsDeleted]  bit           NOT NULL CONSTRAINT DF_Attendance_IsDeleted DEFAULT (0),
        [DeletedAt]  datetime2(0)  NULL,
        [RowVersion] rowversion    NOT NULL,

        CONSTRAINT PK_Attendance PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Attendance_Schedule UNIQUE ([ScheduleId]),
        CONSTRAINT CK_Attendance_Status CHECK ([Status] IN (0, 1)),
        CONSTRAINT CK_Attendance_CheckOut CHECK ([CheckOutAt] IS NULL OR [CheckOutAt] > [CheckInAt]),
        CONSTRAINT FK_Attendance_Schedule FOREIGN KEY ([ScheduleId]) REFERENCES dbo.[Schedule] ([Id]),
        CONSTRAINT FK_Attendance_ApprovedBy FOREIGN KEY ([ApprovedBy]) REFERENCES dbo.[Employee] ([Id])
    );

    CREATE INDEX IX_Attendance_Status_ApprovedAt ON dbo.[Attendance] ([Status], [ApprovedAt]);
END
GO

IF OBJECT_ID(N'dbo.UniformItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[UniformItem]
    (
        [Id]         int           IDENTITY(1,1) NOT NULL,
        [Code]       varchar(20)   NOT NULL,
        [Name]       nvarchar(100) NOT NULL,
        [Unit]       nvarchar(20)  NULL,
        [IsActive]   bit           NOT NULL CONSTRAINT DF_UniformItem_IsActive DEFAULT (1),

        [CreatedAt]  datetime2(0)  NOT NULL CONSTRAINT DF_UniformItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int           NULL,
        [UpdatedAt]  datetime2(0)  NULL,
        [UpdatedBy]  int           NULL,
        [IsDeleted]  bit           NOT NULL CONSTRAINT DF_UniformItem_IsDeleted DEFAULT (0),
        [DeletedAt]  datetime2(0)  NULL,
        [RowVersion] rowversion    NOT NULL,

        CONSTRAINT PK_UniformItem PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_UniformItem_Code UNIQUE ([Code])
    );
END
GO

IF OBJECT_ID(N'dbo.UniformIssue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[UniformIssue]
    (
        [Id]            int           IDENTITY(1,1) NOT NULL,
        [EmployeeId]    int           NOT NULL,
        [UniformItemId] int           NOT NULL,
        [Quantity]      int           NOT NULL,
        [IssuedAt]      date          NOT NULL,
        [ReturnedAt]    date          NULL,
        [Status]        tinyint       NOT NULL CONSTRAINT DF_UniformIssue_Status DEFAULT (0),
        [Note]          nvarchar(255) NULL,

        [CreatedAt]     datetime2(0)  NOT NULL CONSTRAINT DF_UniformIssue_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]     int           NULL,
        [UpdatedAt]     datetime2(0)  NULL,
        [UpdatedBy]     int           NULL,
        [IsDeleted]     bit           NOT NULL CONSTRAINT DF_UniformIssue_IsDeleted DEFAULT (0),
        [DeletedAt]     datetime2(0)  NULL,
        [RowVersion]    rowversion    NOT NULL,

        CONSTRAINT PK_UniformIssue PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT CK_UniformIssue_Quantity CHECK ([Quantity] > 0),
        CONSTRAINT CK_UniformIssue_Status CHECK ([Status] IN (0, 1, 2)),
        CONSTRAINT CK_UniformIssue_Returned CHECK ([ReturnedAt] IS NULL OR [ReturnedAt] >= [IssuedAt]),
        CONSTRAINT CK_UniformIssue_ReturnedStatus CHECK ([Status] <> 1 OR [ReturnedAt] IS NOT NULL),
        CONSTRAINT FK_UniformIssue_Employee FOREIGN KEY ([EmployeeId]) REFERENCES dbo.[Employee] ([Id]),
        CONSTRAINT FK_UniformIssue_Item FOREIGN KEY ([UniformItemId]) REFERENCES dbo.[UniformItem] ([Id])
    );
END
GO

/* =====================================================================================
   3. NHÓM BÁN HÀNG
   ===================================================================================== */

IF OBJECT_ID(N'dbo.Area', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Area]
    (
        [Id]           int          IDENTITY(1,1) NOT NULL,
        [Name]         nvarchar(50) NOT NULL,
        [DisplayOrder] int          NOT NULL CONSTRAINT DF_Area_DisplayOrder DEFAULT (0),
        [IsActive]     bit          NOT NULL CONSTRAINT DF_Area_IsActive DEFAULT (1),

        [CreatedAt]    datetime2(0) NOT NULL CONSTRAINT DF_Area_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]    int          NULL,
        [UpdatedAt]    datetime2(0) NULL,
        [UpdatedBy]    int          NULL,
        [IsDeleted]    bit          NOT NULL CONSTRAINT DF_Area_IsDeleted DEFAULT (0),
        [DeletedAt]    datetime2(0) NULL,
        [RowVersion]   rowversion   NOT NULL,

        CONSTRAINT PK_Area PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Area_Name UNIQUE ([Name])
    );
END
GO

IF OBJECT_ID(N'dbo.DiningTable', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DiningTable]
    (
        [Id]          int          IDENTITY(1,1) NOT NULL,
        [TableNumber] nvarchar(10) NOT NULL,
        [AreaId]      int          NULL,
        [MaxSeats]    int          NOT NULL,
        [IsActive]    bit          NOT NULL CONSTRAINT DF_DiningTable_IsActive DEFAULT (1),

        [CreatedAt]   datetime2(0) NOT NULL CONSTRAINT DF_DiningTable_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]   int          NULL,
        [UpdatedAt]   datetime2(0) NULL,
        [UpdatedBy]   int          NULL,
        [IsDeleted]   bit          NOT NULL CONSTRAINT DF_DiningTable_IsDeleted DEFAULT (0),
        [DeletedAt]   datetime2(0) NULL,
        [RowVersion]  rowversion   NOT NULL,

        CONSTRAINT PK_DiningTable PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_DiningTable_TableNumber UNIQUE ([TableNumber]),
        CONSTRAINT CK_DiningTable_MaxSeats CHECK ([MaxSeats] > 0),
        CONSTRAINT FK_DiningTable_Area FOREIGN KEY ([AreaId]) REFERENCES dbo.[Area] ([Id]) ON DELETE SET NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Category', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Category]
    (
        [Id]           int           IDENTITY(1,1) NOT NULL,
        [Code]         varchar(20)   NOT NULL,
        [Name]         nvarchar(100) NOT NULL,
        [DisplayOrder] int           NOT NULL CONSTRAINT DF_Category_DisplayOrder DEFAULT (0),
        [IsActive]     bit           NOT NULL CONSTRAINT DF_Category_IsActive DEFAULT (1),

        [CreatedAt]    datetime2(0)  NOT NULL CONSTRAINT DF_Category_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]    int           NULL,
        [UpdatedAt]    datetime2(0)  NULL,
        [UpdatedBy]    int           NULL,
        [IsDeleted]    bit           NOT NULL CONSTRAINT DF_Category_IsDeleted DEFAULT (0),
        [DeletedAt]    datetime2(0)  NULL,
        [RowVersion]   rowversion    NOT NULL,

        CONSTRAINT PK_Category PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Category_Code UNIQUE ([Code]),
        CONSTRAINT UQ_Category_Name UNIQUE ([Name])
    );
END
GO

IF OBJECT_ID(N'dbo.MenuItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[MenuItem]
    (
        [Id]          int            IDENTITY(1,1) NOT NULL,
        [Code]        varchar(20)    NOT NULL,
        [Name]        nvarchar(200)  NOT NULL,
        [CategoryId]  int            NOT NULL,
        [Price]       decimal(18,2)  NOT NULL,
        [Unit]        nvarchar(20)   NULL,
        [Description] nvarchar(500)  NULL,
        [ImageUrl]    nvarchar(255)  NULL,
        [IsAvailable] bit            NOT NULL CONSTRAINT DF_MenuItem_IsAvailable DEFAULT (1),

        [CreatedAt]   datetime2(0)   NOT NULL CONSTRAINT DF_MenuItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]   int            NULL,
        [UpdatedAt]   datetime2(0)   NULL,
        [UpdatedBy]   int            NULL,
        [IsDeleted]   bit            NOT NULL CONSTRAINT DF_MenuItem_IsDeleted DEFAULT (0),
        [DeletedAt]   datetime2(0)   NULL,
        [RowVersion]  rowversion     NOT NULL,

        CONSTRAINT PK_MenuItem PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_MenuItem_Code UNIQUE ([Code]),
        CONSTRAINT CK_MenuItem_Price CHECK ([Price] >= 0),
        CONSTRAINT FK_MenuItem_Category FOREIGN KEY ([CategoryId]) REFERENCES dbo.[Category] ([Id])
    );

    CREATE INDEX IX_MenuItem_Category_Available ON dbo.[MenuItem] ([CategoryId], [IsAvailable]);
END
GO

IF OBJECT_ID(N'dbo.Invoice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Invoice]
    (
        [Id]                 int            IDENTITY(1,1) NOT NULL,
        [InvoiceCode]        varchar(20)    NOT NULL,
        [CustomerCount]      int            NOT NULL,
        [OpenedByEmployeeId] int            NOT NULL,
        [OpenedAt]           datetime2(0)   NOT NULL CONSTRAINT DF_Invoice_OpenedAt DEFAULT (SYSUTCDATETIME()),
        [ClosedAt]           datetime2(0)   NULL,
        [ShiftId]            int            NULL,
        [DiscountAmount]     decimal(18,2)  NOT NULL CONSTRAINT DF_Invoice_Discount DEFAULT (0),
        [Status]             tinyint        NOT NULL CONSTRAINT DF_Invoice_Status DEFAULT (0),
        [Note]               nvarchar(255)  NULL,

        [CreatedAt]          datetime2(0)   NOT NULL CONSTRAINT DF_Invoice_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]          int            NULL,
        [UpdatedAt]          datetime2(0)   NULL,
        [UpdatedBy]          int            NULL,
        [IsDeleted]          bit            NOT NULL CONSTRAINT DF_Invoice_IsDeleted DEFAULT (0),
        [DeletedAt]          datetime2(0)   NULL,
        [RowVersion]         rowversion     NOT NULL,

        CONSTRAINT PK_Invoice PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Invoice_Code UNIQUE ([InvoiceCode]),
        CONSTRAINT CK_Invoice_CustomerCount CHECK ([CustomerCount] > 0),
        CONSTRAINT CK_Invoice_Discount CHECK ([DiscountAmount] >= 0),
        CONSTRAINT CK_Invoice_Status CHECK ([Status] IN (0, 1, 2)),
        CONSTRAINT CK_Invoice_ClosedAt CHECK ([ClosedAt] IS NULL OR [ClosedAt] >= [OpenedAt]),
        CONSTRAINT CK_Invoice_PaidHasClosedAt CHECK ([Status] <> 1 OR [ClosedAt] IS NOT NULL),
        CONSTRAINT FK_Invoice_Employee FOREIGN KEY ([OpenedByEmployeeId]) REFERENCES dbo.[Employee] ([Id]),
        CONSTRAINT FK_Invoice_Shift FOREIGN KEY ([ShiftId]) REFERENCES dbo.[Shift] ([Id])
    );

    CREATE INDEX IX_Invoice_Status_ClosedAt ON dbo.[Invoice] ([Status], [ClosedAt]);
    CREATE INDEX IX_Invoice_Employee_OpenedAt ON dbo.[Invoice] ([OpenedByEmployeeId], [OpenedAt]);
    CREATE INDEX IX_Invoice_Shift_ClosedAt ON dbo.[Invoice] ([ShiftId], [ClosedAt]);
END
GO

IF OBJECT_ID(N'dbo.InvoiceTable', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[InvoiceTable]
    (
        [Id]         int          IDENTITY(1,1) NOT NULL,
        [InvoiceId]  int          NOT NULL,
        [TableId]    int          NOT NULL,
        [AssignedAt] datetime2(0) NOT NULL CONSTRAINT DF_InvoiceTable_AssignedAt DEFAULT (SYSUTCDATETIME()),
        [ReleasedAt] datetime2(0) NULL,
        [IsPrimary]  bit          NOT NULL CONSTRAINT DF_InvoiceTable_IsPrimary DEFAULT (0),

        [CreatedAt]  datetime2(0) NOT NULL CONSTRAINT DF_InvoiceTable_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]  int          NULL,
        [UpdatedAt]  datetime2(0) NULL,
        [UpdatedBy]  int          NULL,
        [IsDeleted]  bit          NOT NULL CONSTRAINT DF_InvoiceTable_IsDeleted DEFAULT (0),
        [DeletedAt]  datetime2(0) NULL,
        [RowVersion] rowversion   NOT NULL,

        CONSTRAINT PK_InvoiceTable PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT CK_InvoiceTable_ReleasedAt CHECK ([ReleasedAt] IS NULL OR [ReleasedAt] >= [AssignedAt]),
        CONSTRAINT FK_InvoiceTable_Invoice FOREIGN KEY ([InvoiceId]) REFERENCES dbo.[Invoice] ([Id]),
        CONSTRAINT FK_InvoiceTable_Table FOREIGN KEY ([TableId]) REFERENCES dbo.[DiningTable] ([Id])
    );

    CREATE UNIQUE INDEX UX_InvoiceTable_ActiveTable ON dbo.[InvoiceTable] ([TableId]) WHERE [ReleasedAt] IS NULL;
    CREATE INDEX IX_InvoiceTable_Invoice ON dbo.[InvoiceTable] ([InvoiceId]);
END
GO

IF OBJECT_ID(N'dbo.InvoiceDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[InvoiceDetail]
    (
        [Id]           int            IDENTITY(1,1) NOT NULL,
        [InvoiceId]    int            NOT NULL,
        [MenuItemId]   int            NOT NULL,
        [Quantity]     int            NOT NULL,
        [UnitPrice]    decimal(18,2)  NOT NULL,
        [Note]         nvarchar(255)  NULL,
        [Status]       tinyint        NOT NULL CONSTRAINT DF_InvoiceDetail_Status DEFAULT (0),
        [OrderedAt]    datetime2(0)   NOT NULL CONSTRAINT DF_InvoiceDetail_OrderedAt DEFAULT (SYSUTCDATETIME()),
        [CompletedAt]  datetime2(0)   NULL,
        [CancelledAt]  datetime2(0)   NULL,
        [CancelReason] nvarchar(255)  NULL,
        [CancelledBy]  int            NULL,

        [CreatedAt]    datetime2(0)   NOT NULL CONSTRAINT DF_InvoiceDetail_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]    int            NULL,
        [UpdatedAt]    datetime2(0)   NULL,
        [UpdatedBy]    int            NULL,
        [IsDeleted]    bit            NOT NULL CONSTRAINT DF_InvoiceDetail_IsDeleted DEFAULT (0),
        [DeletedAt]    datetime2(0)   NULL,
        [RowVersion]   rowversion     NOT NULL,

        CONSTRAINT PK_InvoiceDetail PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT CK_InvoiceDetail_Quantity CHECK ([Quantity] > 0),
        CONSTRAINT CK_InvoiceDetail_UnitPrice CHECK ([UnitPrice] >= 0),
        CONSTRAINT CK_InvoiceDetail_Status CHECK ([Status] IN (0, 1, 2, 3)),
        CONSTRAINT CK_InvoiceDetail_CompletedAt CHECK ([Status] <> 2 OR [CompletedAt] IS NOT NULL),
        CONSTRAINT CK_InvoiceDetail_Cancel CHECK ([Status] <> 3 OR ([CancelledAt] IS NOT NULL AND [CancelReason] IS NOT NULL)),
        CONSTRAINT FK_InvoiceDetail_Invoice FOREIGN KEY ([InvoiceId]) REFERENCES dbo.[Invoice] ([Id]),
        CONSTRAINT FK_InvoiceDetail_MenuItem FOREIGN KEY ([MenuItemId]) REFERENCES dbo.[MenuItem] ([Id]),
        CONSTRAINT FK_InvoiceDetail_CancelledBy FOREIGN KEY ([CancelledBy]) REFERENCES dbo.[Employee] ([Id])
    );

    CREATE INDEX IX_InvoiceDetail_Invoice_Status ON dbo.[InvoiceDetail] ([InvoiceId], [Status]);
    CREATE INDEX IX_InvoiceDetail_Status_OrderedAt ON dbo.[InvoiceDetail] ([Status], [OrderedAt]);
END
GO

IF OBJECT_ID(N'dbo.TableTransferLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TableTransferLog]
    (
        [Id]                   int           IDENTITY(1,1) NOT NULL,
        [InvoiceId]            int           NOT NULL,
        [FromTableId]          int           NOT NULL,
        [ToTableId]            int           NOT NULL,
        [MovedAt]              datetime2(0)  NOT NULL CONSTRAINT DF_TableTransferLog_MovedAt DEFAULT (SYSUTCDATETIME()),
        [MovedByEmployeeId]    int           NOT NULL,
        [Note]                 nvarchar(255) NULL,

        [CreatedAt]            datetime2(0)  NOT NULL CONSTRAINT DF_TableTransferLog_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]            int           NULL,
        [UpdatedAt]            datetime2(0)  NULL,
        [UpdatedBy]            int           NULL,
        [IsDeleted]            bit           NOT NULL CONSTRAINT DF_TableTransferLog_IsDeleted DEFAULT (0),
        [DeletedAt]            datetime2(0)  NULL,
        [RowVersion]           rowversion    NOT NULL,

        CONSTRAINT PK_TableTransferLog PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT CK_TableTransferLog_Different CHECK ([FromTableId] <> [ToTableId]),
        CONSTRAINT FK_TableTransferLog_Invoice FOREIGN KEY ([InvoiceId]) REFERENCES dbo.[Invoice] ([Id]),
        CONSTRAINT FK_TableTransferLog_FromTable FOREIGN KEY ([FromTableId]) REFERENCES dbo.[DiningTable] ([Id]),
        CONSTRAINT FK_TableTransferLog_ToTable FOREIGN KEY ([ToTableId]) REFERENCES dbo.[DiningTable] ([Id]),
        CONSTRAINT FK_TableTransferLog_Employee FOREIGN KEY ([MovedByEmployeeId]) REFERENCES dbo.[Employee] ([Id])
    );

    CREATE INDEX IX_TableTransferLog_Invoice ON dbo.[TableTransferLog] ([InvoiceId]);
    CREATE INDEX IX_TableTransferLog_MovedAt ON dbo.[TableTransferLog] ([MovedAt]);
END
GO

IF OBJECT_ID(N'dbo.Payment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Payment]
    (
        [Id]                int            IDENTITY(1,1) NOT NULL,
        [InvoiceId]         int            NOT NULL,
        [Method]            tinyint        NOT NULL,
        [Amount]            decimal(18,2)  NOT NULL,
        [PaidAt]            datetime2(0)   NOT NULL CONSTRAINT DF_Payment_PaidAt DEFAULT (SYSUTCDATETIME()),
        [PaidByEmployeeId]  int            NOT NULL,
        [Note]              nvarchar(255)  NULL,

        [CreatedAt]         datetime2(0)   NOT NULL CONSTRAINT DF_Payment_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]         int            NULL,
        [UpdatedAt]         datetime2(0)   NULL,
        [UpdatedBy]         int            NULL,
        [IsDeleted]         bit            NOT NULL CONSTRAINT DF_Payment_IsDeleted DEFAULT (0),
        [DeletedAt]         datetime2(0)   NULL,
        [RowVersion]        rowversion     NOT NULL,

        CONSTRAINT PK_Payment PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Payment_Invoice UNIQUE ([InvoiceId]),
        CONSTRAINT CK_Payment_Method CHECK ([Method] IN (0, 1)),
        CONSTRAINT CK_Payment_Amount CHECK ([Amount] >= 0),
        CONSTRAINT FK_Payment_Invoice FOREIGN KEY ([InvoiceId]) REFERENCES dbo.[Invoice] ([Id]),
        CONSTRAINT FK_Payment_Employee FOREIGN KEY ([PaidByEmployeeId]) REFERENCES dbo.[Employee] ([Id])
    );

    CREATE INDEX IX_Payment_PaidAt ON dbo.[Payment] ([PaidAt]);
    CREATE INDEX IX_Payment_Method_PaidAt ON dbo.[Payment] ([Method], [PaidAt]);
END
GO

/* =====================================================================================
   4. NHÓM HỆ THỐNG
   ===================================================================================== */

IF OBJECT_ID(N'dbo.SystemParameter', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[SystemParameter]
    (
        [ParamKey]    varchar(50)    NOT NULL,
        [ParamValue]  nvarchar(255)  NOT NULL,
        [DataType]    varchar(20)    NOT NULL,
        [Description] nvarchar(255)  NULL,

        [CreatedAt]   datetime2(0)   NOT NULL CONSTRAINT DF_SystemParameter_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]   int            NULL,
        [UpdatedAt]   datetime2(0)   NULL,
        [UpdatedBy]   int            NULL,
        [IsDeleted]   bit            NOT NULL CONSTRAINT DF_SystemParameter_IsDeleted DEFAULT (0),
        [DeletedAt]   datetime2(0)   NULL,
        [RowVersion]  rowversion     NOT NULL,

        CONSTRAINT PK_SystemParameter PRIMARY KEY CLUSTERED ([ParamKey]),
        CONSTRAINT CK_SystemParameter_DataType CHECK ([DataType] IN ('int', 'decimal', 'string', 'bool', 'date'))
    );
END
GO

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[AuditLog]
    (
        [Id]              bigint         IDENTITY(1,1) NOT NULL,
        [TableName]       nvarchar(100)  NOT NULL,
        [RecordId]        nvarchar(50)   NOT NULL,
        [Action]          varchar(10)    NOT NULL,
        [OldValues]       nvarchar(max)  NULL,
        [NewValues]       nvarchar(max)  NULL,
        [ChangedByUserId] int            NULL,
        [ChangedAt]       datetime2(0)   NOT NULL CONSTRAINT DF_AuditLog_ChangedAt DEFAULT (SYSUTCDATETIME()),
        [ClientIp]        varchar(45)    NULL,

        CONSTRAINT PK_AuditLog PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT CK_AuditLog_Action CHECK ([Action] IN ('INSERT', 'UPDATE', 'DELETE')),
        CONSTRAINT FK_AuditLog_AppUser FOREIGN KEY ([ChangedByUserId]) REFERENCES dbo.[AppUser] ([Id])
    );

    CREATE INDEX IX_AuditLog_Table_Record_ChangedAt ON dbo.[AuditLog] ([TableName], [RecordId], [ChangedAt]);
END
GO

PRINT N'01_schema.sql: hoàn tất tạo 25 bảng.';
GO
