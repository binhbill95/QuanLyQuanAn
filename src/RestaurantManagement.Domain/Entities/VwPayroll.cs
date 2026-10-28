using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class VwPayroll
{
    public int? Year { get; set; }

    public int? Month { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string PositionName { get; set; } = null!;

    public int? ApprovedShiftCount { get; set; }

    public decimal ShiftRate { get; set; }

    public decimal? Salary { get; set; }
}
