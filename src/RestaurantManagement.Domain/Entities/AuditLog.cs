using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class AuditLog
{
    public long Id { get; set; }

    public string TableName { get; set; } = null!;

    public string RecordId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public int? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ClientIp { get; set; }

    public virtual AppUser? ChangedByUser { get; set; }
}
