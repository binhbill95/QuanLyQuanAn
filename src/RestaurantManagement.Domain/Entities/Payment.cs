using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public byte Method { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public int PaidByEmployeeId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Employee PaidByEmployee { get; set; } = null!;
}
