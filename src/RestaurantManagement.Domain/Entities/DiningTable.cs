using System;
using System.Collections.Generic;

namespace RestaurantManagement.Domain.Entities;

public partial class DiningTable
{
    public int Id { get; set; }

    public string TableNumber { get; set; } = null!;

    public int? AreaId { get; set; }

    public int MaxSeats { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Area? Area { get; set; }

    public virtual InvoiceTable? InvoiceTable { get; set; }

    public virtual ICollection<TableTransferLog> TableTransferLogFromTables { get; set; } = new List<TableTransferLog>();

    public virtual ICollection<TableTransferLog> TableTransferLogToTables { get; set; } = new List<TableTransferLog>();
}
