using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwRevenueByPeriod
{
    public int? Year { get; set; }

    public int? Quarter { get; set; }

    public int? Month { get; set; }

    public int? InvoiceCount { get; set; }

    public int? CustomerCount { get; set; }

    public decimal? Revenue { get; set; }
}
