using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Models;

public sealed record StockBalanceItem(
    Guid ProductId, string? ProductCode, string ProductName, string CategoryName,
    string BaseUnitName, decimal QuantityBase, decimal MinimumQuantity,
    decimal AverageCost, decimal StockValue, int AvailableSerials,
    Guid? CategoryId = null, string? PrimaryBarcode = null, string? ImagePath = null,
    string? MainSupplierName = null, decimal? SalePrice = null)
{
    public bool IsLowStock => QuantityBase <= MinimumQuantity;
    public bool IsOutOfStock => QuantityBase <= 0;
    public string StockStatus => QuantityBase < 0
        ? "رصيد سالب"
        : IsOutOfStock
            ? "نفد المخزون"
            : IsLowStock
                ? "منخفض"
                : "متوفر";
}

public sealed record StockMovementListItem(
    Guid Id, DateTime CreatedAt, string ProductName, string UnitName,
    decimal QuantityBase, StockMovementType MovementType, string? ReferenceType,
    string? Notes, string? UserName)
{
    public string MovementTypeName => MovementType switch
    {
        StockMovementType.Purchase => "شراء",
        StockMovementType.Sale => "بيع",
        StockMovementType.SalesReturn => "مرتجع بيع",
        StockMovementType.PurchaseReturn => "مرتجع شراء",
        StockMovementType.Adjustment => "تعديل يدوي",
        StockMovementType.Damage => "تالف",
        StockMovementType.InventoryCount => "تسوية جرد",
        _ => "رصيد افتتاحي"
    };
}

public sealed record InventoryCountListItem(
    Guid Id, string CountNo, string CountScope, string Status, int ItemsCount,
    decimal TotalAbsoluteDifference, string? UserName, DateTime CreatedAt, DateTime? ConfirmedAt);

public sealed class InventoryCountRequest
{
    public string CountScope { get; init; } = "full";
    public string? Notes { get; init; }
    public IReadOnlyCollection<InventoryCountLineInput> Items { get; init; } = Array.Empty<InventoryCountLineInput>();
}

public sealed record InventoryCountLineInput(
    Guid ProductId, decimal SystemQuantityBase, decimal ActualQuantityBase, string? Notes);

public sealed class StockAdjustmentRequest
{
    public Guid ProductId { get; init; }
    /// <summary>add, remove, or set.</summary>
    public string AdjustmentType { get; init; } = "add";
    public decimal QuantityBase { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ReferenceNo { get; init; }
    public string? Notes { get; init; }
}

public sealed record StockAdjustmentResult(
    Guid MovementId,
    decimal PreviousBalance,
    decimal QuantityDifference,
    decimal NewBalance);

public sealed record AlertListItem(
    Guid Id, string AlertType, string Title, string Message, AlertSeverity Severity,
    bool IsRead, string? ReferenceType, Guid? ReferenceId, DateTime CreatedAt, DateTime? ReadAt);
