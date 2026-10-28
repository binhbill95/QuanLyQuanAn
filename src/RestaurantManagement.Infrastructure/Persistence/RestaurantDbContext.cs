using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Infrastructure.Persistence;

public partial class RestaurantDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUser;

    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
        : base(options)
    {
    }

    public RestaurantDbContext(
        DbContextOptions<RestaurantDbContext> options,
        ICurrentUserService currentUser)
        : this(options)
    {
        _currentUser = currentUser;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var connection = Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;
        if (closeConnection)
        {
            connection.Open();
        }

        try
        {
            SetAuditUser(connection, Database.CurrentTransaction?.GetDbTransaction(), _currentUser?.UserId);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        finally
        {
            try
            {
                SetAuditUser(connection, Database.CurrentTransaction?.GetDbTransaction(), null);
            }
            finally
            {
                if (closeConnection)
                {
                    connection.Close();
                }
            }
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;
        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await SetAuditUserAsync(
                connection,
                Database.CurrentTransaction?.GetDbTransaction(),
                _currentUser?.UserId,
                cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        finally
        {
            try
            {
                await SetAuditUserAsync(
                    connection,
                    Database.CurrentTransaction?.GetDbTransaction(),
                    null,
                    CancellationToken.None);
            }
            finally
            {
                if (closeConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }

    private static void SetAuditUser(
        DbConnection connection,
        DbTransaction? transaction,
        int? userId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'ChangedByUserId', @value=@userId;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@userId";
        parameter.DbType = DbType.Int32;
        parameter.Value = userId.HasValue ? userId.Value : DBNull.Value;
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();
    }

    private static async Task SetAuditUserAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int? userId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'ChangedByUserId', @value=@userId;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@userId";
        parameter.DbType = DbType.Int32;
        parameter.Value = userId.HasValue ? userId.Value : DBNull.Value;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<DiningTable> DiningTables { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeShift> EmployeeShifts { get; set; }

    public virtual DbSet<EmployeeSkill> EmployeeSkills { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<InvoiceTable> InvoiceTables { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SystemParameter> SystemParameters { get; set; }

    public virtual DbSet<TableTransferLog> TableTransferLogs { get; set; }

    public virtual DbSet<UniformIssue> UniformIssues { get; set; }

    public virtual DbSet<UniformItem> UniformItems { get; set; }

    public virtual DbSet<VwApprovedShiftCount> VwApprovedShiftCounts { get; set; }

    public virtual DbSet<VwCustomerByEmployee> VwCustomerByEmployees { get; set; }

    public virtual DbSet<VwCustomerByShift> VwCustomerByShifts { get; set; }

    public virtual DbSet<VwCustomerByTable> VwCustomerByTables { get; set; }

    public virtual DbSet<VwDiningTableStatus> VwDiningTableStatuses { get; set; }

    public virtual DbSet<VwInvoiceSummary> VwInvoiceSummaries { get; set; }

    public virtual DbSet<VwItemSale> VwItemSales { get; set; }

    public virtual DbSet<VwKitchenQueue> VwKitchenQueues { get; set; }

    public virtual DbSet<VwPayroll> VwPayrolls { get; set; }

    public virtual DbSet<VwRevenueByPeriod> VwRevenueByPeriods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUser", tb => tb.HasTrigger("trg_AppUser_Audit"));

            entity.HasIndex(e => e.Username, "UQ_AppUser_Username").IsUnique();

            entity.HasIndex(e => e.EmployeeId, "UX_AppUser_EmployeeId")
                .IsUnique()
                .HasFilter("([EmployeeId] IS NOT NULL)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AppUser_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_AppUser_IsActive");
            entity.Property(e => e.LastLoginAt).HasPrecision(0);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Employee).WithOne(p => p.AppUser)
                .HasForeignKey<AppUser>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AppUser_Employee");

            entity.HasOne(d => d.Role).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppUser_Role");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("Area");

            entity.HasIndex(e => e.Name, "UQ_Area_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Area_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Area_IsActive");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendance", tb => tb.HasTrigger("trg_Attendance_Audit"));

            entity.HasIndex(e => new { e.Status, e.ApprovedAt }, "IX_Attendance_Status_ApprovedAt");

            entity.HasIndex(e => e.ScheduleId, "UQ_Attendance_Schedule").IsUnique();

            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CheckInAt).HasPrecision(0);
            entity.Property(e => e.CheckOutAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Attendance_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Attendance_ApprovedBy");

            entity.HasOne(d => d.Schedule).WithOne(p => p.Attendance)
                .HasForeignKey<Attendance>(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Schedule");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.HasIndex(e => new { e.TableName, e.RecordId, e.ChangedAt }, "IX_AuditLog_Table_Record_ChangedAt");

            entity.Property(e => e.Action)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AuditLog_ChangedAt");
            entity.Property(e => e.ClientIp)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.RecordId).HasMaxLength(50);
            entity.Property(e => e.TableName).HasMaxLength(100);

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_AuditLog_AppUser");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category", tb => tb.HasTrigger("trg_Category_Audit"));

            entity.HasIndex(e => e.Code, "UQ_Category_Code").IsUnique();

            entity.HasIndex(e => e.Name, "UQ_Category_Name").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Category_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Category_IsActive");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.ToTable("DiningTable");

            entity.HasIndex(e => e.TableNumber, "UQ_DiningTable_TableNumber").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_DiningTable_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_DiningTable_IsActive");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TableNumber).HasMaxLength(10);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Area).WithMany(p => p.DiningTables)
                .HasForeignKey(d => d.AreaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_DiningTable_Area");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee", tb => tb.HasTrigger("trg_Employee_Audit"));

            entity.HasIndex(e => e.EmployeeCode, "UQ_Employee_Code").IsUnique();

            entity.HasIndex(e => e.IdentityNumber, "UQ_Employee_IdentityNumber").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Employee_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdentityNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ShiftRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Position).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employee_Position");
        });

        modelBuilder.Entity<EmployeeShift>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.ShiftId });

            entity.ToTable("EmployeeShift");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_EmployeeShift_CreatedAt");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeShifts)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeShift_Employee");

            entity.HasOne(d => d.Shift).WithMany(p => p.EmployeeShifts)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeShift_Shift");
        });

        modelBuilder.Entity<EmployeeSkill>(entity =>
        {
            entity.HasKey(e => new { e.EmployeeId, e.SkillId });

            entity.ToTable("EmployeeSkill");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_EmployeeSkill_CreatedAt");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeSkills)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeSkill_Employee");

            entity.HasOne(d => d.Skill).WithMany(p => p.EmployeeSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeSkill_Skill");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoice", tb => tb.HasTrigger("trg_Invoice_Audit"));

            entity.HasIndex(e => new { e.OpenedByEmployeeId, e.OpenedAt }, "IX_Invoice_Employee_OpenedAt");

            entity.HasIndex(e => new { e.ShiftId, e.ClosedAt }, "IX_Invoice_Shift_ClosedAt");

            entity.HasIndex(e => new { e.Status, e.ClosedAt }, "IX_Invoice_Status_ClosedAt");

            entity.HasIndex(e => e.InvoiceCode, "UQ_Invoice_Code").IsUnique();

            entity.Property(e => e.ClosedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Invoice_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.OpenedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Invoice_OpenedAt");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.OpenedByEmployee).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.OpenedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Employee");

            entity.HasOne(d => d.Shift).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("FK_Invoice_Shift");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.ToTable("InvoiceDetail", tb =>
                {
                    tb.HasTrigger("trg_InvoiceDetail_Audit");
                    tb.HasTrigger("trg_InvoiceDetail_CancelGuard");
                    tb.HasTrigger("trg_InvoiceDetail_InvoiceStatusGuard");
                });

            entity.HasIndex(e => new { e.InvoiceId, e.Status }, "IX_InvoiceDetail_Invoice_Status");

            entity.HasIndex(e => new { e.Status, e.OrderedAt }, "IX_InvoiceDetail_Status_OrderedAt");

            entity.Property(e => e.CancelReason).HasMaxLength(255);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_InvoiceDetail_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.OrderedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_InvoiceDetail_OrderedAt");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("FK_InvoiceDetail_CancelledBy");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceDetail_Invoice");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceDetail_MenuItem");
        });

        modelBuilder.Entity<InvoiceTable>(entity =>
        {
            entity.ToTable("InvoiceTable");

            entity.HasIndex(e => e.InvoiceId, "IX_InvoiceTable_Invoice");

            entity.HasIndex(e => e.TableId, "UX_InvoiceTable_ActiveTable")
                .IsUnique()
                .HasFilter("([ReleasedAt] IS NULL)");

            entity.Property(e => e.AssignedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_InvoiceTable_AssignedAt");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_InvoiceTable_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.ReleasedAt).HasPrecision(0);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceTables)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceTable_Invoice");

            entity.HasOne(d => d.Table).WithOne(p => p.InvoiceTable)
                .HasForeignKey<InvoiceTable>(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceTable_Table");
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("MenuItem", tb => tb.HasTrigger("trg_MenuItem_Audit"));

            entity.HasIndex(e => new { e.CategoryId, e.IsAvailable }, "IX_MenuItem_Category_Available");

            entity.HasIndex(e => e.Code, "UQ_MenuItem_Code").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_MenuItem_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true, "DF_MenuItem_IsAvailable");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MenuItem_Category");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment", tb => tb.HasTrigger("trg_Payment_Audit"));

            entity.HasIndex(e => new { e.Method, e.PaidAt }, "IX_Payment_Method_PaidAt");

            entity.HasIndex(e => e.PaidAt, "IX_Payment_PaidAt");

            entity.HasIndex(e => e.InvoiceId, "UQ_Payment_Invoice").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Payment_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PaidAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Payment_PaidAt");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Invoice).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Invoice");

            entity.HasOne(d => d.PaidByEmployee).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaidByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Employee");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permission");

            entity.HasIndex(e => e.Code, "UQ_Permission_Code").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Permission_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.ModuleName).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("Position");

            entity.HasIndex(e => e.Code, "UQ_Position_Code").IsUnique();

            entity.HasIndex(e => e.Name, "UQ_Position_Name").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Position_CreatedAt");
            entity.Property(e => e.DefaultShiftRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Position_IsActive");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.DefaultRole).WithMany(p => p.Positions)
                .HasForeignKey(d => d.DefaultRoleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Position_Role");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.HasIndex(e => e.Code, "UQ_Role_Code").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Role_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.ToTable("RolePermission");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_RolePermission_CreatedAt");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Permission");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Role");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("Schedule", tb => tb.HasTrigger("trg_Schedule_Audit"));

            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }, "IX_Schedule_Employee_WorkDate");

            entity.HasIndex(e => new { e.WorkDate, e.ShiftId }, "IX_Schedule_WorkDate_Shift");

            entity.HasIndex(e => new { e.EmployeeId, e.ShiftId, e.WorkDate }, "UQ_Schedule_Employee_Shift_Date").IsUnique();

            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Schedule_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.ScheduleApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Schedule_ApprovedBy");

            entity.HasOne(d => d.Employee).WithMany(p => p.ScheduleEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedule_Employee");

            entity.HasOne(d => d.Shift).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedule_Shift");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.ToTable("Shift", tb => tb.HasTrigger("trg_Shift_Audit"));

            entity.HasIndex(e => e.Name, "UQ_Shift_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Shift_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Shift_IsActive");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("Skill");

            entity.HasIndex(e => e.Name, "UQ_Skill_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Skill_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Skill_IsActive");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<SystemParameter>(entity =>
        {
            entity.HasKey(e => e.ParamKey);

            entity.ToTable("SystemParameter");

            entity.Property(e => e.ParamKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SystemParameter_CreatedAt");
            entity.Property(e => e.DataType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ParamValue).HasMaxLength(255);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<TableTransferLog>(entity =>
        {
            entity.ToTable("TableTransferLog");

            entity.HasIndex(e => e.InvoiceId, "IX_TableTransferLog_Invoice");

            entity.HasIndex(e => e.MovedAt, "IX_TableTransferLog_MovedAt");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_TableTransferLog_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.MovedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_TableTransferLog_MovedAt");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.FromTable).WithMany(p => p.TableTransferLogFromTables)
                .HasForeignKey(d => d.FromTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TableTransferLog_FromTable");

            entity.HasOne(d => d.Invoice).WithMany(p => p.TableTransferLogs)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TableTransferLog_Invoice");

            entity.HasOne(d => d.MovedByEmployee).WithMany(p => p.TableTransferLogs)
                .HasForeignKey(d => d.MovedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TableTransferLog_Employee");

            entity.HasOne(d => d.ToTable).WithMany(p => p.TableTransferLogToTables)
                .HasForeignKey(d => d.ToTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TableTransferLog_ToTable");
        });

        modelBuilder.Entity<UniformIssue>(entity =>
        {
            entity.ToTable("UniformIssue");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_UniformIssue_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Employee).WithMany(p => p.UniformIssues)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UniformIssue_Employee");

            entity.HasOne(d => d.UniformItem).WithMany(p => p.UniformIssues)
                .HasForeignKey(d => d.UniformItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UniformIssue_Item");
        });

        modelBuilder.Entity<UniformItem>(entity =>
        {
            entity.ToTable("UniformItem");

            entity.HasIndex(e => e.Code, "UQ_UniformItem_Code").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_UniformItem_CreatedAt");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_UniformItem_IsActive");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<VwApprovedShiftCount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ApprovedShiftCount");

            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwCustomerByEmployee>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CustomerByEmployee");

            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwCustomerByShift>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CustomerByShift");

            entity.Property(e => e.ShiftName).HasMaxLength(50);
        });

        modelBuilder.Entity<VwCustomerByTable>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CustomerByTable");

            entity.Property(e => e.TableNumber).HasMaxLength(10);
        });

        modelBuilder.Entity<VwDiningTableStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DiningTableStatus");

            entity.Property(e => e.AreaName).HasMaxLength(50);
            entity.Property(e => e.StatusName).HasMaxLength(12);
            entity.Property(e => e.TableNumber).HasMaxLength(10);
        });

        modelBuilder.Entity<VwInvoiceSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_InvoiceSummary");

            entity.Property(e => e.ClosedAt).HasPrecision(0);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OpenedAt).HasPrecision(0);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<VwItemSale>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ItemSales");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.ItemCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.Revenue).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<VwKitchenQueue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_KitchenQueue");

            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ItemCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.OrderedAt).HasPrecision(0);
            entity.Property(e => e.TableNumber).HasMaxLength(10);
        });

        modelBuilder.Entity<VwPayroll>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Payroll");

            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PositionName).HasMaxLength(50);
            entity.Property(e => e.Salary).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ShiftRate).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VwRevenueByPeriod>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RevenueByPeriod");

            entity.Property(e => e.Revenue).HasColumnType("decimal(38, 4)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
