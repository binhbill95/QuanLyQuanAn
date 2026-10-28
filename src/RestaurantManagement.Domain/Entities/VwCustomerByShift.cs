using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwCustomerByShift
{
    public int? Year { get; set; }

    public int? Quarter { get; set; }

    public int? Month { get; set; }

    public int ShiftId { get; set; }

    public string ShiftName { get; set; } = null!;

    public int? InvoiceCount { get; set; }

    public int? CustomerCount { get; set; }
}
