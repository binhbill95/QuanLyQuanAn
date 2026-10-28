using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class EmployeeShift
{
    public int EmployeeId { get; set; }

    public int ShiftId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Shift Shift { get; set; } = null!;
}
