using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class InvoiceTable
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int TableId { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual DiningTable Table { get; set; } = null!;
}
