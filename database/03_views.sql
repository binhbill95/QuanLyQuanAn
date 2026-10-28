/* =====================================================================================
   Restaurant Management System — SQL Server 2022
   File    : 03_views.sql
   Mục đích: 10 view phục vụ dữ liệu suy diễn + báo cáo (không lưu dữ liệu dư thừa)
   Chạy    : sqlcmd -S localhost,1433 -U sa -P <password> -C -b -i 03_views.sql
   ===================================================================================== */

SET NOCOUNT ON;
GO

USE [RestaurantManagement];
GO

/* -------------------------------------------------------------------------------------
   1. vw_InvoiceSummary — tạm tính / giảm giá / thành tiền của mỗi hóa đơn
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_InvoiceSummary
AS
WITH DetailTotal AS
(
    SELECT d.[InvoiceId],
           SUM(CAST(d.[Quantity] AS decimal(18,2)) * d.[UnitPrice]) AS [SubTotal],
           SUM(d.[Quantity])                                        AS [TotalQuantity]
    FROM dbo.[InvoiceDetail] d
    WHERE d.[Status] <> 3 AND d.[IsDeleted] = 0
    GROUP BY d.[InvoiceId]
)
SELECT i.[Id]                  AS [InvoiceId],
       i.[InvoiceCode],
       i.[CustomerCount],
       i.[OpenedByEmployeeId],
       i.[OpenedAt],
       i.[ClosedAt],
       i.[ShiftId],
       i.[Status],
       i.[DiscountAmount],
       ISNULL(t.[SubTotal], 0)                          AS [SubTotal],
       ISNULL(t.[TotalQuantity], 0)                     AS [TotalQuantity],
       ISNULL(t.[SubTotal], 0) - i.[DiscountAmount]     AS [TotalAmount]
FROM dbo.[Invoice] i
LEFT JOIN DetailTotal t ON t.[InvoiceId] = i.[Id]
WHERE i.[IsDeleted] = 0;
GO

/* -------------------------------------------------------------------------------------
   2. vw_DiningTableStatus — trạng thái bàn suy diễn:
      Trống / Có khách / Đang đợi món / Món đã xong
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_DiningTableStatus
AS
WITH ActiveInvoice AS
(
    SELECT it.[TableId],
           i.[Id] AS [InvoiceId],
           SUM(CASE WHEN d.[Status] IN (0, 1) THEN 1 ELSE 0 END) AS [PendingCount],
           SUM(CASE WHEN d.[Status] = 2 THEN 1 ELSE 0 END)       AS [DoneCount],
           COUNT(d.[Id])                                          AS [TotalCount]
    FROM dbo.[InvoiceTable] it
    JOIN dbo.[Invoice] i        ON i.[Id] = it.[InvoiceId] AND i.[Status] = 0 AND i.[IsDeleted] = 0
    LEFT JOIN dbo.[InvoiceDetail] d
           ON d.[InvoiceId] = i.[Id] AND d.[Status] <> 3 AND d.[IsDeleted] = 0
    WHERE it.[ReleasedAt] IS NULL AND it.[IsDeleted] = 0
    GROUP BY it.[TableId], i.[Id]
)
SELECT t.[Id]           AS [TableId],
       t.[TableNumber],
       a.[Id]           AS [AreaId],
       a.[Name]         AS [AreaName],
       t.[MaxSeats],
       ai.[InvoiceId],
       CASE
           WHEN ai.[InvoiceId] IS NULL                               THEN N'Trống'
           WHEN ai.[PendingCount] > 0                                THEN N'Đang đợi món'
           WHEN ai.[TotalCount] > 0
                AND ai.[DoneCount] = ai.[TotalCount]                 THEN N'Món đã xong'
           ELSE N'Có khách'
       END              AS [StatusName]
FROM dbo.[DiningTable] t
LEFT JOIN dbo.[Area] a ON a.[Id] = t.[AreaId]
LEFT JOIN ActiveInvoice ai ON ai.[TableId] = t.[Id]
WHERE t.[IsDeleted] = 0 AND t.[IsActive] = 1;
GO

/* -------------------------------------------------------------------------------------
   3. vw_KitchenQueue — hàng đợi món tại bếp, sắp xếp FIFO theo thời gian đặt
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_KitchenQueue
AS
SELECT d.[Id]                AS [InvoiceDetailId],
       d.[InvoiceId],
       i.[InvoiceCode],
       t.[TableNumber],
       d.[MenuItemId],
       m.[Code]              AS [ItemCode],
       m.[Name]              AS [ItemName],
       d.[Quantity],
       d.[Note],
       d.[Status],
       d.[OrderedAt],
       CASE WHEN DATEDIFF(SECOND, d.[OrderedAt], SYSUTCDATETIME()) < 0 THEN 0
            ELSE DATEDIFF(SECOND, d.[OrderedAt], SYSUTCDATETIME()) / 60
       END                    AS [WaitingMinutes]
FROM dbo.[InvoiceDetail] d
JOIN dbo.[Invoice] i   ON i.[Id] = d.[InvoiceId] AND i.[Status] = 0 AND i.[IsDeleted] = 0
JOIN dbo.[MenuItem] m  ON m.[Id] = d.[MenuItemId]
LEFT JOIN dbo.[InvoiceTable] it
       ON it.[InvoiceId] = i.[Id] AND it.[IsPrimary] = 1 AND it.[ReleasedAt] IS NULL AND it.[IsDeleted] = 0
LEFT JOIN dbo.[DiningTable] t ON t.[Id] = it.[TableId]
WHERE d.[Status] IN (0, 1) AND d.[IsDeleted] = 0;
GO

/* -------------------------------------------------------------------------------------
   4. vw_RevenueByPeriod — doanh thu theo năm / quý / tháng
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_RevenueByPeriod
AS
SELECT YEAR(i.[ClosedAt])          AS [Year],
       DATEPART(QUARTER, i.[ClosedAt]) AS [Quarter],
       MONTH(i.[ClosedAt])         AS [Month],
       COUNT(DISTINCT i.[Id])      AS [InvoiceCount],
       SUM(i.[CustomerCount])      AS [CustomerCount],
       SUM(s.[TotalAmount])        AS [Revenue]
FROM dbo.[Invoice] i
JOIN dbo.vw_InvoiceSummary s ON s.[InvoiceId] = i.[Id]
WHERE i.[Status] = 1 AND i.[IsDeleted] = 0 AND i.[ClosedAt] IS NOT NULL
GROUP BY YEAR(i.[ClosedAt]), DATEPART(QUARTER, i.[ClosedAt]), MONTH(i.[ClosedAt]);
GO

/* -------------------------------------------------------------------------------------
   5. vw_CustomerByShift — số khách theo ca (tháng / quý / năm)
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_CustomerByShift
AS
SELECT YEAR(i.[ClosedAt])             AS [Year],
       DATEPART(QUARTER, i.[ClosedAt]) AS [Quarter],
       MONTH(i.[ClosedAt])            AS [Month],
       s.[Id]                         AS [ShiftId],
       s.[Name]                       AS [ShiftName],
       COUNT(DISTINCT i.[Id])         AS [InvoiceCount],
       SUM(i.[CustomerCount])         AS [CustomerCount]
FROM dbo.[Invoice] i
JOIN dbo.[Shift] s ON s.[Id] = i.[ShiftId]
WHERE i.[Status] = 1 AND i.[IsDeleted] = 0 AND i.[ClosedAt] IS NOT NULL
GROUP BY YEAR(i.[ClosedAt]), DATEPART(QUARTER, i.[ClosedAt]), MONTH(i.[ClosedAt]), s.[Id], s.[Name];
GO

/* -------------------------------------------------------------------------------------
   6. vw_CustomerByEmployee — số khách theo nhân viên phục vụ (tuần)
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_CustomerByEmployee
AS
SELECT YEAR(i.[ClosedAt])                    AS [Year],
       DATEPART(ISO_WEEK, i.[ClosedAt])      AS [Week],
       MIN(CAST(i.[ClosedAt] AS date))       AS [WeekStartDate],
       e.[Id]                                AS [EmployeeId],
       e.[EmployeeCode],
       e.[FullName],
       COUNT(DISTINCT i.[Id])                AS [InvoiceCount],
       SUM(i.[CustomerCount])                AS [CustomerCount]
FROM dbo.[Invoice] i
JOIN dbo.[Employee] e ON e.[Id] = i.[OpenedByEmployeeId]
WHERE i.[Status] = 1 AND i.[IsDeleted] = 0 AND i.[ClosedAt] IS NOT NULL
GROUP BY YEAR(i.[ClosedAt]), DATEPART(ISO_WEEK, i.[ClosedAt]), e.[Id], e.[EmployeeCode], e.[FullName];
GO

/* -------------------------------------------------------------------------------------
   7. vw_CustomerByTable — số khách của bàn (tháng / quý / năm)
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_CustomerByTable
AS
SELECT YEAR(i.[ClosedAt])             AS [Year],
       DATEPART(QUARTER, i.[ClosedAt]) AS [Quarter],
       MONTH(i.[ClosedAt])            AS [Month],
       t.[Id]                         AS [TableId],
       t.[TableNumber],
       COUNT(DISTINCT i.[Id])         AS [InvoiceCount],
       SUM(i.[CustomerCount])         AS [CustomerCount]
FROM dbo.[Invoice] i
JOIN dbo.[InvoiceTable] it ON it.[InvoiceId] = i.[Id] AND it.[IsDeleted] = 0
JOIN dbo.[DiningTable] t   ON t.[Id] = it.[TableId]
WHERE i.[Status] = 1 AND i.[IsDeleted] = 0 AND i.[ClosedAt] IS NOT NULL
GROUP BY YEAR(i.[ClosedAt]), DATEPART(QUARTER, i.[ClosedAt]), MONTH(i.[ClosedAt]), t.[Id], t.[TableNumber];
GO

/* -------------------------------------------------------------------------------------
   8. vw_ItemSales — số lượng bán và doanh thu theo món (tháng / quý / năm)
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_ItemSales
AS
SELECT YEAR(i.[ClosedAt])                  AS [Year],
       DATEPART(QUARTER, i.[ClosedAt])     AS [Quarter],
       MONTH(i.[ClosedAt])                 AS [Month],
       m.[Id]                              AS [MenuItemId],
       m.[Code]                            AS [ItemCode],
       m.[Name]                            AS [ItemName],
       c.[Name]                            AS [CategoryName],
       SUM(d.[Quantity])                   AS [Quantity],
       SUM(CAST(d.[Quantity] AS decimal(18,2)) * d.[UnitPrice]) AS [Revenue]
FROM dbo.[InvoiceDetail] d
JOIN dbo.[Invoice] i   ON i.[Id] = d.[InvoiceId] AND i.[Status] = 1 AND i.[IsDeleted] = 0
JOIN dbo.[MenuItem] m  ON m.[Id] = d.[MenuItemId]
JOIN dbo.[Category] c  ON c.[Id] = m.[CategoryId]
WHERE d.[Status] <> 3 AND d.[IsDeleted] = 0 AND i.[ClosedAt] IS NOT NULL
GROUP BY YEAR(i.[ClosedAt]), DATEPART(QUARTER, i.[ClosedAt]), MONTH(i.[ClosedAt]),
         m.[Id], m.[Code], m.[Name], c.[Name];
GO

/* -------------------------------------------------------------------------------------
   9. vw_ApprovedShiftCount — số ca đã duyệt theo nhân viên / tháng
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_ApprovedShiftCount
AS
SELECT YEAR(sc.[WorkDate])   AS [Year],
       MONTH(sc.[WorkDate])  AS [Month],
       e.[Id]                AS [EmployeeId],
       e.[EmployeeCode],
       e.[FullName],
       COUNT(a.[Id])         AS [ApprovedShiftCount]
FROM dbo.[Attendance] a
JOIN dbo.[Schedule] sc ON sc.[Id] = a.[ScheduleId]
JOIN dbo.[Employee] e  ON e.[Id] = sc.[EmployeeId]
WHERE a.[Status] = 1 AND a.[IsDeleted] = 0 AND sc.[IsDeleted] = 0
GROUP BY YEAR(sc.[WorkDate]), MONTH(sc.[WorkDate]), e.[Id], e.[EmployeeCode], e.[FullName];
GO

/* -------------------------------------------------------------------------------------
   10. vw_Payroll — lương tháng = số ca đã duyệt × đơn giá ca hiệu dụng
       Đơn giá hiệu dụng = Employee.ShiftRate (nếu có) hoặc Position.DefaultShiftRate
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_Payroll
AS
SELECT s.[Year],
       s.[Month],
       s.[EmployeeId],
       s.[EmployeeCode],
       s.[FullName],
       p.[Name]                                                   AS [PositionName],
       s.[ApprovedShiftCount],
       ISNULL(e.[ShiftRate], p.[DefaultShiftRate])                AS [ShiftRate],
       s.[ApprovedShiftCount] * ISNULL(e.[ShiftRate], p.[DefaultShiftRate]) AS [Salary]
FROM dbo.vw_ApprovedShiftCount s
JOIN dbo.[Employee] e ON e.[Id] = s.[EmployeeId]
JOIN dbo.[Position] p ON p.[Id] = e.[PositionId];
GO

PRINT N'03_views.sql: hoàn tất tạo 10 view.';
GO
