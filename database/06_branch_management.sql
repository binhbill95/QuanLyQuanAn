/* Additive migration for existing databases; safe to re-run. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [RestaurantManagement];
GO

IF OBJECT_ID(N'dbo.Branch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Branch]
    (
        [Id]        int           IDENTITY(1,1) NOT NULL,
        [Code]      varchar(20)   NOT NULL,
        [Name]      nvarchar(100) NOT NULL,
        [Address]   nvarchar(255) NULL,
        [Phone]     nvarchar(20)  NULL,
        [IsActive]  bit           NOT NULL CONSTRAINT DF_Branch_IsActive DEFAULT (1),
        [CreatedAt] datetime2(0)  NOT NULL CONSTRAINT DF_Branch_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] int           NULL,
        [UpdatedAt] datetime2(0)  NULL,
        [UpdatedBy] int           NULL,
        [IsDeleted] bit           NOT NULL CONSTRAINT DF_Branch_IsDeleted DEFAULT (0),
        [DeletedAt] datetime2(0)  NULL,
        [RowVersion] rowversion   NOT NULL,
        CONSTRAINT PK_Branch PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_Branch_Code UNIQUE ([Code])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.[Branch] WHERE [Code] = 'CN001')
    INSERT INTO dbo.[Branch] ([Code], [Name])
    VALUES ('CN001', N'Chi nhánh chính');

IF NOT EXISTS (SELECT 1 FROM dbo.[Branch] WHERE [Code] = 'CN002')
    INSERT INTO dbo.[Branch] ([Code], [Name])
    VALUES ('CN002', N'Chi nhánh 2');
GO

IF COL_LENGTH(N'dbo.AppUser', N'BranchId') IS NULL
    ALTER TABLE dbo.[AppUser] ADD [BranchId] int NULL;

IF COL_LENGTH(N'dbo.Employee', N'BranchId') IS NULL
    ALTER TABLE dbo.[Employee] ADD [BranchId] int NULL;

IF COL_LENGTH(N'dbo.DiningTable', N'BranchId') IS NULL
    ALTER TABLE dbo.[DiningTable] ADD [BranchId] int NULL;

IF COL_LENGTH(N'dbo.Invoice', N'BranchId') IS NULL
    ALTER TABLE dbo.[Invoice] ADD [BranchId] int NULL;
GO

/* Batches above add the columns; the variable and backfill must compile after the columns exist. */
DECLARE @DefaultBranchId int =
    (SELECT [Id] FROM dbo.[Branch] WHERE [Code] = 'CN001');

UPDATE u
SET [BranchId] = @DefaultBranchId
FROM dbo.[AppUser] u
INNER JOIN dbo.[Role] r ON r.[Id] = u.[RoleId]
WHERE u.[BranchId] IS NULL AND r.[Code] <> 'ADMIN';

UPDATE dbo.[Employee]
SET [BranchId] = @DefaultBranchId
WHERE [BranchId] IS NULL;

UPDATE dbo.[DiningTable]
SET [BranchId] = @DefaultBranchId
WHERE [BranchId] IS NULL;

UPDATE i
SET [BranchId] = COALESCE(tableBranch.[BranchId], @DefaultBranchId)
FROM dbo.[Invoice] i
OUTER APPLY
(
    SELECT TOP (1) dt.[BranchId]
    FROM dbo.[InvoiceTable] it
    INNER JOIN dbo.[DiningTable] dt ON dt.[Id] = it.[TableId]
    WHERE it.[InvoiceId] = i.[Id]
    ORDER BY it.[AssignedAt], it.[Id]
) AS tableBranch
WHERE i.[BranchId] IS NULL;
GO

IF COL_LENGTH(N'dbo.DiningTable', N'BranchId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.[DiningTable] WHERE [BranchId] IS NULL)
    THROW 51000, 'BranchId backfill failed for DiningTable.', 1;

IF COL_LENGTH(N'dbo.Employee', N'BranchId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.[Employee] WHERE [BranchId] IS NULL)
    THROW 51000, 'BranchId backfill failed for Employee.', 1;

IF COL_LENGTH(N'dbo.Invoice', N'BranchId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.[Invoice] WHERE [BranchId] IS NULL)
    THROW 51000, 'BranchId backfill failed for Invoice.', 1;
GO

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.Employee')
      AND [name] = N'BranchId'
      AND [is_nullable] = 1
)
    ALTER TABLE dbo.[Employee] ALTER COLUMN [BranchId] int NOT NULL;

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.DiningTable')
      AND [name] = N'BranchId'
      AND [is_nullable] = 1
)
    ALTER TABLE dbo.[DiningTable] ALTER COLUMN [BranchId] int NOT NULL;

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'dbo.Invoice')
      AND [name] = N'BranchId'
      AND [is_nullable] = 1
)
    ALTER TABLE dbo.[Invoice] ALTER COLUMN [BranchId] int NOT NULL;
GO

/* Số bàn chỉ cần duy nhất trong từng chi nhánh. */
IF EXISTS
(
    SELECT 1 FROM sys.key_constraints
    WHERE [name] = N'UQ_DiningTable_TableNumber'
      AND [parent_object_id] = OBJECT_ID(N'dbo.DiningTable')
)
    ALTER TABLE dbo.[DiningTable] DROP CONSTRAINT UQ_DiningTable_TableNumber;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.key_constraints
    WHERE [name] = N'UQ_DiningTable_Branch_TableNumber'
      AND [parent_object_id] = OBJECT_ID(N'dbo.DiningTable')
)
    ALTER TABLE dbo.[DiningTable]
        ADD CONSTRAINT UQ_DiningTable_Branch_TableNumber UNIQUE ([BranchId], [TableNumber]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_AppUser_Branch')
    ALTER TABLE dbo.[AppUser] ADD CONSTRAINT FK_AppUser_Branch
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branch] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_DiningTable_Branch')
    ALTER TABLE dbo.[DiningTable] ADD CONSTRAINT FK_DiningTable_Branch
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branch] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Employee_Branch')
    ALTER TABLE dbo.[Employee] ADD CONSTRAINT FK_Employee_Branch
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branch] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Invoice_Branch')
    ALTER TABLE dbo.[Invoice] ADD CONSTRAINT FK_Invoice_Branch
        FOREIGN KEY ([BranchId]) REFERENCES dbo.[Branch] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_DiningTable_BranchId'
               AND [object_id] = OBJECT_ID(N'dbo.DiningTable'))
    CREATE INDEX IX_DiningTable_BranchId ON dbo.[DiningTable] ([BranchId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Employee_BranchId'
               AND [object_id] = OBJECT_ID(N'dbo.Employee'))
    CREATE INDEX IX_Employee_BranchId ON dbo.[Employee] ([BranchId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Invoice_BranchId'
               AND [object_id] = OBJECT_ID(N'dbo.Invoice'))
    CREATE INDEX IX_Invoice_BranchId ON dbo.[Invoice] ([BranchId]);
GO
