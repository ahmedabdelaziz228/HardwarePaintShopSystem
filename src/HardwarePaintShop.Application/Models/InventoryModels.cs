using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Models;

public sealed record StockBalanceItem(
    Guid ProductId, string? ProductCode, string ProductName, string CategoryName,
    string BaseUnitName, decimal QuantityBase, decimal MinimumQuantity,
    decimal AverageCost, decimal StockValue, int AvailableSerials)
{
    public bool IsLowStock => QuantityBase <= MinimumQuantity;
    public string StockStatus => QuantityBase < 0 ? "رصيد سالب" : IsLowStock ? "تحت الحد" : "متاح";
}

public sealed record StockMovementListItem(
    Guid Id, DateTime CreatedAt, string ProductName, string UnitName,
    decimal QuantityBase, StockMovementType MovementType, string? ReferenceType,
    string? Notes, string? UserName);

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

public sealed record AlertListItem(
    Guid Id, string AlertType, string Title, string Message, AlertSeverity Severity,
    bool IsRead, string? ReferenceType, Guid? ReferenceId, DateTime CreatedAt, DateTime? ReadAt);
