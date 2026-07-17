namespace HardwarePaintShop.Domain.Entities;

/// <summary>
/// Customer (retail or business). Balance is maintained via CustomerTransaction records.
/// Do NOT update CurrentBalance directly — always go through transaction records.
/// </summary>
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>Customer classification (e.g. "retail", "wholesale", "contractor").</summary>
    public string? CustomerType { get; set; }

    public Guid? PriceGroupId { get; set; }
    public PriceGroup? PriceGroup { get; set; }

    /// <summary>Maximum allowed credit (0 = no credit).</summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Running balance. Positive = customer owes us money (debit).
    /// Derived from CustomerTransaction records — do not update manually.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<CustomerTransaction> Transactions { get; set; } = new List<CustomerTransaction>();
}

/// <summary>
/// Supplier (vendor). Balance is maintained via SupplierTransaction records.
/// Do NOT update CurrentBalance directly — always go through transaction records.
/// </summary>
public class Supplier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// Running balance. Positive = we owe supplier money (credit).
    /// Derived from SupplierTransaction records — do not update manually.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<SupplierTransaction> Transactions { get; set; } = new List<SupplierTransaction>();
}
