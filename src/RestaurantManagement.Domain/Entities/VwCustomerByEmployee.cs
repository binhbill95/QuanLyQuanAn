using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwCustomerByEmployee
{
    public int? Year { get; set; }

    public int? Week { get; set; }

    public DateOnly? WeekStartDate { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int? InvoiceCount { get; set; }

    public int? CustomerCount { get; set; }
}
