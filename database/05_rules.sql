/* =====================================================================================
   Restaurant Management System — SQL Server 2022
   File    : 05_rules.sql
   Mục đích: Các ràng buộc nghiệp vụ cần chặn ở tầng CSDL (defense in depth)
   Chạy    : sqlcmd -S localhost,1433 -U sa -P <password> -C -b -i 05_rules.sql
   ===================================================================================== */

SET NOCOUNT ON;
GO

USE [RestaurantManagement];
GO

/* -------------------------------------------------------------------------------------
   R1. Chỉ được hủy món khi món CHƯA được thực hiện (Status = 0 - Đang chờ).
       Không cho chuyển 1 (Đang chế biến) hoặc 2 (Hoàn thành) sang 3 (Đã hủy).
       Lưu ý: quy tắc này cũng được kiểm tra ở Service Layer theo Coding Rules;
              trigger chỉ là lớp bảo vệ cuối cùng.
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_InvoiceDetail_CancelGuard
ON dbo.[InvoiceDetail]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN deleted  d ON d.[Id] = i.[Id]
        WHERE i.[Status] = 3
          AND d.[Status] <> 3
          AND d.[Status] <> 0
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, N'Chỉ được hủy món đang ở trạng thái chờ (Status = 0).', 1;
    END
END
GO

/* -------------------------------------------------------------------------------------
   R2. Không cho thêm món vào hóa đơn đã thanh toán hoặc đã hủy.
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_InvoiceDetail_InvoiceStatusGuard
ON dbo.[InvoiceDetail]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.[Invoice] inv ON inv.[Id] = i.[InvoiceId]
        WHERE inv.[Status] <> 0
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, N'Chỉ được thêm/sửa món trong hóa đơn đang phục vụ (Status = 0).', 1;
    END
END
GO

PRINT N'05_rules.sql: hoàn tất tạo 2 trigger ràng buộc nghiệp vụ.';
GO
