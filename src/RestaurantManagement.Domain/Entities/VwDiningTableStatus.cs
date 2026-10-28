using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwDiningTableStatus
{
    public int TableId { get; set; }

    public string TableNumber { get; set; } = null!;

    public int? AreaId { get; set; }

    public string? AreaName { get; set; }

    public int MaxSeats { get; set; }

    public int? InvoiceId { get; set; }

    public string StatusName { get; set; } = null!;
}
