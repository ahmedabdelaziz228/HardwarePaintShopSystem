using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Models;

public sealed record PurchaseInvoiceListItem(
    Guid Id,
    string InvoiceNo,
    string? SupplierInvoiceNo,
    string SupplierName,
    DateTime InvoiceDate,
    DateTime? DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus,
    InvoiceStatus Status);

public sealed class PurchaseInvoiceDetails
{
    public Guid Id { get; init; }
    public string InvoiceNo { get; init; } = string.Empty;
    public string? SupplierInvoiceNo { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal Subtotal { get; init; }
    public decimal ExtraCosts { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public InvoiceStatus Status { get; init; }
    public string? Notes { get; init; }
    public List<PurchaseInvoiceItemData> Items { get; init; } = new();
}

public sealed class PurchaseInvoiceItemData
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string? ProductCode { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductUnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal ConversionFactorToBase { get; init; }
    public decimal Quantity { get; init; }
    public decimal QuantityBaseUnit { get; init; }
    public decimal UnitPurchasePrice { get; init; }
    public decimal LineTotal { get; init; }
    public bool IsSerialTracked { get; init; }
    public List<string> SerialNumbers { get; init; } = new();
}

public sealed record PurchaseProductOption(
    Guid ProductId,
    Guid ProductUnitId,
    string? ProductCode,
    string ProductName,
    string UnitName,
    decimal ConversionFactorToBase,
    decimal StockBaseQuantity,
    decimal? LastPurchasePriceBaseUnit,
    bool IsSerialTracked,
    string? Barcode)
{
    public string DisplayName => $"{ProductName} — {UnitName}";
    public string ConversionPreview => $"1 {UnitName} = {ConversionFactorToBase:N3} وحدة أساسية";
}

public sealed class PurchaseDraftRequest
{
    public Guid? Id { get; init; }
    public Guid SupplierId { get; init; }
    public string? SupplierInvoiceNo { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal ExtraCosts { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<PurchaseLineInput> Items { get; init; } = Array.Empty<PurchaseLineInput>();
}

public sealed class PurchaseLineInput
{
    public Guid ProductId { get; init; }
    public Guid ProductUnitId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPurchasePrice { get; init; }
    public IReadOnlyCollection<string> SerialNumbers { get; init; } = Array.Empty<string>();
}

public sealed class PostPurchaseRequest
{
    public Guid InvoiceId { get; init; }
    public decimal PaidAmount { get; init; }
    public Guid? CashboxId { get; init; }
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.Cash;
}
