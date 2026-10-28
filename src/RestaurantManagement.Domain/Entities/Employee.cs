using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class Employee
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public byte? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public int PositionId { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string IdentityNumber { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? ShiftRate { get; set; }

    public byte Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual AppUser? AppUser { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<EmployeeShift> EmployeeShifts { get; set; } = new List<EmployeeShift>();

    public virtual ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Position Position { get; set; } = null!;

    public virtual ICollection<Schedule> ScheduleApprovedByNavigations { get; set; } = new List<Schedule>();

    public virtual ICollection<Schedule> ScheduleEmployees { get; set; } = new List<Schedule>();

    public virtual ICollection<TableTransferLog> TableTransferLogs { get; set; } = new List<TableTransferLog>();

    public virtual ICollection<UniformIssue> UniformIssues { get; set; } = new List<UniformIssue>();
}
