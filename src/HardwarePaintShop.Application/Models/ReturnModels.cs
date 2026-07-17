using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Models;

public sealed record ReturnSourceInvoice(
    Guid Id,
    ReturnType ReturnType,
    string InvoiceNo,
    string PartyName,
    DateTime InvoiceDate,
    decimal TotalAmount,
    string Status);

public sealed record ReturnListItem(
    Guid Id,
    string ReturnNo,
    ReturnType ReturnType,
    string PartyName,
    string OriginalInvoiceNo,
    decimal TotalAmount,
    string RefundMethod,
    string Status,
    DateTime CreatedAt);

public sealed class ReturnSourceDetails
{
    public Guid InvoiceId { get; init; }
    public ReturnType ReturnType { get; init; }
    public string InvoiceNo { get; init; } = string.Empty;
    public Guid? PartyId { get; init; }
    public string PartyName { get; init; } = string.Empty;
    public List<ReturnSourceLine> Lines { get; init; } = new();
}

public sealed class ReturnSourceLine
{
    public Guid ProductId { get; init; }
    public Guid ProductUnitId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public decimal ConversionFactorToBase { get; init; }
    public decimal OriginalQuantity { get; init; }
    public decimal AlreadyReturnedQuantity { get; init; }
    public decimal AvailableQuantity => Math.Max(0, OriginalQuantity - AlreadyReturnedQuantity);
    public decimal UnitPrice { get; init; }
    public bool IsSerialTracked { get; init; }
    public IReadOnlyCollection<string> AvailableSerials { get; init; } = Array.Empty<string>();
}

public sealed class PostReturnRequest
{
    public ReturnType ReturnType { get; init; }
    public Guid OriginalInvoiceId { get; init; }
    public string RefundMethod { get; init; } = "cash";
    public Guid? CashboxId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<ReturnLineInput> Items { get; init; } = Array.Empty<ReturnLineInput>();
}

public sealed class ReturnLineInput
{
    public Guid ProductId { get; init; }
    public Guid ProductUnitId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public bool Restock { get; init; } = true;
    public ReturnItemCondition Condition { get; init; } = ReturnItemCondition.Good;
    public IReadOnlyCollection<string> SerialNumbers { get; init; } = Array.Empty<string>();
    public string? Notes { get; init; }
}
