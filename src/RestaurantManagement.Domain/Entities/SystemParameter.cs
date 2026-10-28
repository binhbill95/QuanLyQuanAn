using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class SystemParameter
{
    public string ParamKey { get; set; } = null!;

    public string ParamValue { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
