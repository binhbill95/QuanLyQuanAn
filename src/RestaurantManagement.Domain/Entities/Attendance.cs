using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class Attendance
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public DateTime CheckInAt { get; set; }

    public DateTime? CheckOutAt { get; set; }

    public byte Status { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Employee? ApprovedByNavigation { get; set; }

    public virtual Schedule Schedule { get; set; } = null!;
}
