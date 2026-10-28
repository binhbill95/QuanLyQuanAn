using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class Position
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? DefaultRoleId { get; set; }

    public decimal DefaultShiftRate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Role? DefaultRole { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
