using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class InvoiceDetail
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }

    public byte Status { get; set; }

    public DateTime OrderedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public int? CancelledBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Employee? CancelledByNavigation { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual MenuItem MenuItem { get; set; } = null!;
}
