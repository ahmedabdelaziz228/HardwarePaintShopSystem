using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Interfaces;

/// <summary>
/// Request model for recording a stock movement.
/// </summary>
public record StockMovementRequest(
    Guid ProductId,
    Guid? ProductUnitId,
    decimal QuantityBaseUnit,
    StockMovementType MovementType,
    string? ReferenceType,
    Guid? ReferenceId,
    Guid? UserId,
    string? Notes
);

/// <summary>
/// Service responsible for all stock quantity changes.
/// Stock quantity must NEVER be modified directly on a product.
/// All changes must go through this service to maintain an accurate audit trail.
/// </summary>
public interface IStockMovementService
{
    /// <summary>Records a stock movement. Positive quantity = stock in. Negative = stock out.</summary>
    Task RecordAsync(StockMovementRequest request, CancellationToken ct = default);
}
