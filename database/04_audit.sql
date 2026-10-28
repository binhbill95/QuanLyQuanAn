/* =====================================================================================
   Restaurant Management System — SQL Server 2022
   File    : 04_audit.sql
   Mục đích: Ghi nhật ký thay đổi dữ liệu quan trọng vào dbo.AuditLog (INSERT/UPDATE/DELETE)
   Phạm vi : AppUser, Employee, Category, MenuItem, Shift, Schedule, Attendance,
             Invoice, InvoiceDetail, Payment
   Chạy    : sqlcmd -S localhost,1433 -U sa -P <password> -C -b -i 04_audit.sql
   Lưu ý   : OldValues/NewValues lưu JSON (không có array wrapper) — dùng để truy vết.
             ChangedByUserId lấy từ SESSION_CONTEXT do ứng dụng thiết lập trong SaveChanges.
   ===================================================================================== */

SET NOCOUNT ON;
GO

USE [RestaurantManagement];
GO

/* ---------- Employee ---------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Employee_Audit
ON dbo.[Employee]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog] ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Employee',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- MenuItem ---------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_MenuItem_Audit
ON dbo.[MenuItem]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog] ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'MenuItem',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- Invoice ----------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Invoice_Audit
ON dbo.[Invoice]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog] ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Invoice',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- InvoiceDetail ----------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_InvoiceDetail_Audit
ON dbo.[InvoiceDetail]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog] ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'InvoiceDetail',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- Payment ----------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Payment_Audit
ON dbo.[Payment]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog] ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Payment',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- Category ---------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Category_Audit
ON dbo.[Category]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog]
        ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Category',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- Shift ------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Shift_Audit
ON dbo.[Shift]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog]
        ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Shift',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- Schedule ---------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Schedule_Audit
ON dbo.[Schedule]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog]
        ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Schedule',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- Attendance -------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Attendance_Audit
ON dbo.[Attendance]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.[AuditLog]
        ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'Attendance',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.* FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.* FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

/* ---------- AppUser (exclude credential and login timestamp) ------------------------ */
CREATE OR ALTER TRIGGER dbo.trg_AppUser_Audit
ON dbo.[AppUser]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT (UPDATE([Username]) OR UPDATE([RoleId]) OR UPDATE([EmployeeId]) OR
            UPDATE([IsActive]) OR UPDATE([IsDeleted]) OR UPDATE([PasswordHash]))
        RETURN;

    INSERT INTO dbo.[AuditLog]
        ([TableName], [RecordId], [Action], [OldValues], [NewValues], [ChangedByUserId], [ChangedAt])
    SELECT 'AppUser',
           CAST(COALESCE(i.[Id], d.[Id]) AS nvarchar(50)),
           CASE WHEN d.[Id] IS NULL THEN 'INSERT'
                WHEN i.[Id] IS NULL THEN 'DELETE'
                ELSE 'UPDATE' END,
           CASE WHEN d.[Id] IS NULL THEN NULL
                ELSE (SELECT d2.[Username], d2.[RoleId], d2.[EmployeeId], d2.[IsActive], d2.[IsDeleted],
                             CONVERT(bit, CASE WHEN UPDATE([PasswordHash]) THEN 1 ELSE 0 END) AS [PasswordChanged]
                      FROM deleted d2 WHERE d2.[Id] = d.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.[Id] IS NULL THEN NULL
                ELSE (SELECT i2.[Username], i2.[RoleId], i2.[EmployeeId], i2.[IsActive], i2.[IsDeleted],
                             CONVERT(bit, CASE WHEN UPDATE([PasswordHash]) THEN 1 ELSE 0 END) AS [PasswordChanged]
                      FROM inserted i2 WHERE i2.[Id] = i.[Id]
                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           TRY_CONVERT(int, SESSION_CONTEXT(N'ChangedByUserId')),
           SYSUTCDATETIME()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.[Id] = d.[Id];
END
GO

PRINT N'04_audit.sql: hoàn tất tạo 10 trigger audit.';
GO
