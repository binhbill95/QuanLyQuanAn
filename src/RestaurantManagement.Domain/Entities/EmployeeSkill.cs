using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class EmployeeSkill
{
    public int EmployeeId { get; set; }

    public int SkillId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
