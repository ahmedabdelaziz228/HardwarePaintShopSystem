using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Models;

public sealed record SalesInvoiceListItem(
    Guid Id,
    string InvoiceNo,
    string CustomerName,
    DateTime InvoiceDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus,
    InvoiceStatus Status);

public sealed class SalesInvoiceDetails
{
    public Guid Id { get; init; }
    public string InvoiceNo { get; init; } = string.Empty;
    public Guid? CustomerId { get; init; }
    public string CustomerName { get; init; } = "عميل نقدي";
    public DateTime InvoiceDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal CustomerBalanceBefore { get; init; }
    public decimal CustomerBalanceAfter { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public decimal CostTotal { get; init; }
    public decimal GrossProfit { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public InvoiceStatus Status { get; init; }
    public string? Notes { get; init; }
    public List<SalesInvoiceItemData> Items { get; init; } = new();
}

public sealed class SalesInvoiceItemData
{
    public Guid ProductId { get; init; }
    public string? ProductCode { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Guid ProductUnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal ConversionFactorToBase { get; init; }
    public decimal Quantity { get; init; }
    public decimal QuantityBaseUnit { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public decimal UnitCostBaseAtSale { get; init; }
    public decimal CostTotal { get; init; }
    public decimal GrossProfit { get; init; }
    public bool IsSerialTracked { get; init; }
    public List<string> SerialNumbers { get; init; } = new();
}

public sealed record SalesProductOption(
    Guid ProductId,
    Guid ProductUnitId,
    string? ProductCode,
    string ProductName,
    string UnitName,
    decimal ConversionFactorToBase,
    decimal StockBaseQuantity,
    decimal SalePrice,
    decimal MinSalePrice,
    bool IsSerialTracked,
    string? Barcode)
{
    public string DisplayName => $"{ProductName} — {UnitName}";
    public string StockPreview => $"متاح {StockBaseQuantity:0.###} أساسي";
}

public sealed class SalesDraftRequest
{
    public Guid? Id { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PriceGroupId { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<SalesLineInput> Items { get; init; } = Array.Empty<SalesLineInput>();
}

public sealed class SalesLineInput
{
    public Guid ProductId { get; init; }
    public Guid ProductUnitId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public IReadOnlyCollection<string> SerialNumbers { get; init; } = Array.Empty<string>();
}

public sealed class PostSalesRequest
{
    public Guid InvoiceId { get; init; }
    public decimal PaidAmount { get; init; }
    public Guid? CashboxId { get; init; }
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.Cash;
    /// <summary>
    /// Used only when the invoice has a remaining balance and no existing customer
    /// was selected. The service reuses an existing phone/name match or creates a
    /// debtor customer inside the same posting transaction.
    /// </summary>
    public string? CreditCustomerName { get; init; }
    public string? CreditCustomerPhone { get; init; }
    public string? CreditCustomerAddress { get; init; }
}
