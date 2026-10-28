using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwCustomerByTable
{
    public int? Year { get; set; }

    public int? Quarter { get; set; }

    public int? Month { get; set; }

    public int TableId { get; set; }

    public string TableNumber { get; set; } = null!;

    public int? InvoiceCount { get; set; }

    public int? CustomerCount { get; set; }
}
