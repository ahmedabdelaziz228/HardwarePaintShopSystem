using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Domain.Entities;

/// <summary>
/// Physical or virtual cashbox. Balance is maintained via CashMovement records.
/// IMPORTANT: CurrentBalance must NEVER be edited directly. 
/// All changes must go through CashMovementService.RecordAsync().
/// </summary>
public class Cashbox
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Running balance. Updated transactionally by CashMovementService.
    /// Do NOT modify this field directly.
    /// </summary>
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
}

/// <summary>
/// Record of every cash movement through a cashbox.
/// All amounts are positive; Direction distinguishes In from Out.
/// </summary>
public class CashMovement
{
    public Guid Id { get; set; }
    public Guid CashboxId { get; set; }
    public Cashbox Cashbox { get; set; } = null!;
    public CashMovementType MovementType { get; set; }
    public CashDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shop operating expense (e.g. rent, electricity).
/// </summary>
public class Expense
{
    public Guid Id { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    public Guid CashboxId { get; set; }
    public Cashbox Cashbox { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Owner cash withdrawal from a cashbox.
/// </summary>
public class OwnerWithdrawal
{
    public Guid Id { get; set; }
    public Guid CashboxId { get; set; }
    public Cashbox Cashbox { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime WithdrawalDate { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Inventory stock movement record.
/// IMPORTANT: Stock quantity must NEVER be stored on the product.
/// All stock is tracked exclusively through StockMovements.
/// Positive QuantityBaseUnit = stock coming IN.
/// Negative QuantityBaseUnit = stock going OUT.
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? ProductUnitId { get; set; }
    public ProductUnit? ProductUnit { get; set; }

    /// <summary>
    /// Quantity in the product's base unit.
    /// Positive = stock in, Negative = stock out.
    /// </summary>
    public decimal QuantityBaseUnit { get; set; }
    public StockMovementType MovementType { get; set; }

    /// <summary>Source entity type (e.g. "SalesInvoice", "PurchaseInvoice", "InventoryCount").</summary>
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ledger entry for a customer's account.
/// Debit = customer owes more. Credit = customer balance reduced.
/// </summary>
public class CustomerTransaction
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = "debit"; // debit, credit
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ledger entry for a supplier's account.
/// Debit = we paid supplier. Credit = supplier owes us more.
/// </summary>
public class SupplierTransaction
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = "debit"; // debit, credit
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
