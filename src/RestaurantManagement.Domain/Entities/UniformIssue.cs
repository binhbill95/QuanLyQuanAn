using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class UniformIssue
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int UniformItemId { get; set; }

    public int Quantity { get; set; }

    public DateOnly IssuedAt { get; set; }

    public DateOnly? ReturnedAt { get; set; }

    public byte Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual UniformItem UniformItem { get; set; } = null!;
}
