using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Domain.Entities;

/// <summary>
/// Return header — covers both sales returns and purchase returns.
/// </summary>
public class Return
{
    public Guid Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public ReturnType ReturnType { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? OriginalSalesInvoiceId { get; set; }
    public SalesInvoice? OriginalSalesInvoice { get; set; }

    public Guid? OriginalPurchaseInvoiceId { get; set; }
    public PurchaseInvoice? OriginalPurchaseInvoice { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>"cash", "customer_balance", or "supplier_balance"</summary>
    public string? RefundMethod { get; set; }
    public string Status { get; set; } = "active";

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}

/// <summary>
/// Line item within a return document.
/// </summary>
public class ReturnItem
{
    public Guid Id { get; set; }
    public Guid ReturnId { get; set; }
    public Return Return { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid ProductUnitId { get; set; }
    public ProductUnit ProductUnit { get; set; } = null!;
    public decimal Quantity { get; set; }

    /// <summary>Quantity in base unit for stock calculations.</summary>
    public decimal QuantityBaseUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public ReturnItemCondition Condition { get; set; } = ReturnItemCondition.Good;

    /// <summary>Whether to put the item back into stock.</summary>
    public bool Restock { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// Physical inventory count session.
/// </summary>
public class InventoryCount
{
    public Guid Id { get; set; }
    public string CountNo { get; set; } = string.Empty;

    /// <summary>"full", "category", or "product"</summary>
    public string CountScope { get; set; } = "full";

    /// <summary>"draft", "confirmed", or "cancelled"</summary>
    public string Status { get; set; } = "draft";

    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public ICollection<InventoryCountItem> Items { get; set; } = new List<InventoryCountItem>();
}

/// <summary>
/// Individual product line within an inventory count.
/// DifferenceBase is stored (not computed) so historical counts are preserved.
/// </summary>
public class InventoryCountItem
{
    public Guid Id { get; set; }
    public Guid InventoryCountId { get; set; }
    public InventoryCount InventoryCount { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>System-calculated quantity (in base unit) at time of count.</summary>
    public decimal SystemQuantityBase { get; set; }

    /// <summary>Physically counted quantity (in base unit).</summary>
    public decimal ActualQuantityBase { get; set; }

    /// <summary>Actual minus System — stored for historical accuracy.</summary>
    public decimal DifferenceBase { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// System notification / alert for users or roles.
/// </summary>
public class Alert
{
    public Guid Id { get; set; }

    /// <summary>Type tag (e.g. "low_stock", "credit_limit", "overdue_invoice", "backup_failed").</summary>
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;
    public bool IsRead { get; set; }

    /// <summary>Target individual user (null = broadcast to role).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Target role (null = all users or specific user).</summary>
    public Guid? RoleId { get; set; }

    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Database backup operation log.
/// </summary>
public class BackupLog
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;

    /// <summary>"auto", "manual", or "before_restore"</summary>
    public string BackupType { get; set; } = string.Empty;

    /// <summary>"success" or "failed"</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Registered device that can sync with this installation.
/// </summary>
public class SyncDevice
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Log entry for a synchronization operation.
/// </summary>
public class SyncLog
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public SyncDevice SyncDevice { get; set; } = null!;
    public SyncDirection Direction { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Immutable audit trail record for sensitive data changes.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public AuditAction Action { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
