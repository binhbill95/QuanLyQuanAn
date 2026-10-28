using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwItemSale
{
    public int? Year { get; set; }

    public int? Quarter { get; set; }

    public int? Month { get; set; }

    public int MenuItemId { get; set; }

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public int? Quantity { get; set; }

    public decimal? Revenue { get; set; }
}
