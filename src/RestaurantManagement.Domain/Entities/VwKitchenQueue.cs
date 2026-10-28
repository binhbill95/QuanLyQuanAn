using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwKitchenQueue
{
    public int InvoiceDetailId { get; set; }

    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public string? TableNumber { get; set; }

    public int MenuItemId { get; set; }

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public byte Status { get; set; }

    public DateTime OrderedAt { get; set; }

    public int? WaitingMinutes { get; set; }
}
