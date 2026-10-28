using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwInvoiceSummary
{
    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int CustomerCount { get; set; }

    public int OpenedByEmployeeId { get; set; }

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public int? ShiftId { get; set; }

    public byte Status { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal SubTotal { get; set; }

    public int TotalQuantity { get; set; }

    public decimal? TotalAmount { get; set; }
}
