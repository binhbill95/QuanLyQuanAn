using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class TableTransferLog
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int FromTableId { get; set; }

    public int ToTableId { get; set; }

    public DateTime MovedAt { get; set; }

    public int MovedByEmployeeId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual DiningTable FromTable { get; set; } = null!;

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Employee MovedByEmployee { get; set; } = null!;

    public virtual DiningTable ToTable { get; set; } = null!;
}
