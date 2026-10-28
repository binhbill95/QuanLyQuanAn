using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class Invoice
{
    public int Id { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int CustomerCount { get; set; }

    public int OpenedByEmployeeId { get; set; }

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public int? ShiftId { get; set; }

    public decimal DiscountAmount { get; set; }

    public byte Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<InvoiceTable> InvoiceTables { get; set; } = new List<InvoiceTable>();

    public virtual Employee OpenedByEmployee { get; set; } = null!;

    public virtual Payment? Payment { get; set; }

    public virtual Shift? Shift { get; set; }

    public virtual ICollection<TableTransferLog> TableTransferLogs { get; set; } = new List<TableTransferLog>();
}
