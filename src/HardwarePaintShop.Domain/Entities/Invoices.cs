using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Domain.Entities;

/// <summary>
/// Sales invoice header.
/// IMPORTANT: Invoices must NEVER be deleted — only voided via Status = Voided with audit log.
/// </summary>
public class SalesInvoice
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    /// <summary>Customer ledger balance immediately before posting this invoice.</summary>
    public decimal CustomerBalanceBefore { get; set; }
    /// <summary>Customer ledger balance immediately after posting this invoice.</summary>
    public decimal CustomerBalanceAfter { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Active;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
}

/// <summary>
/// Line item within a sales invoice.
/// </summary>
public class SalesInvoiceItem
{
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid ProductUnitId { get; set; }
    public ProductUnit ProductUnit { get; set; } = null!;
    public decimal Quantity { get; set; }

    /// <summary>Quantity converted to the product's base unit for stock calculations.</summary>
    public decimal QuantityBaseUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Weighted-average cost per base unit captured when the sale is posted.</summary>
    public decimal UnitCostBaseAtSale { get; set; }

    /// <summary>Historical cost of this line at posting time.</summary>
    public decimal CostTotal { get; set; }

    /// <summary>Line revenue after its share of invoice discount, less CostTotal.</summary>
    public decimal GrossProfit { get; set; }

    /// <summary>Serial number string (if the product is serial-tracked).</summary>
    public string? SerialNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Purchase invoice header.
/// IMPORTANT: Invoices must NEVER be deleted — only voided via Status = Voided with audit log.
/// </summary>
public class PurchaseInvoice
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string? SupplierInvoiceNo { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ExtraCosts { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Active;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
}

/// <summary>
/// Line item within a purchase invoice.
/// </summary>
public class PurchaseInvoiceItem
{
    public Guid Id { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid ProductUnitId { get; set; }
    public ProductUnit ProductUnit { get; set; } = null!;
    public decimal Quantity { get; set; }

    /// <summary>Quantity converted to the product's base unit for stock calculations.</summary>
    public decimal QuantityBaseUnit { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Payment record against a sales or purchase invoice.
/// </summary>
public class InvoicePayment
{
    public Guid Id { get; set; }
    public string PaymentNo { get; set; } = string.Empty;

    /// <summary>"sales" or "purchase"</summary>
    public string PaymentType { get; set; } = "sales";
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? CashboxId { get; set; }
    public Cashbox? Cashbox { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
