using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Interfaces;

/// <summary>
/// Request model for recording a cash movement.
/// </summary>
public record CashMovementRequest(
    Guid CashboxId,
    CashMovementType MovementType,
    CashDirection Direction,
    decimal Amount,
    string? ReferenceType,
    Guid? ReferenceId,
    Guid? UserId,
    string? Notes
);

/// <summary>
/// Service responsible for all cashbox balance changes.
/// Cashbox.CurrentBalance must NEVER be modified directly.
/// All changes must go through this service to maintain consistency.
/// </summary>
public interface ICashMovementService
{
    /// <summary>
    /// Records a cash movement and atomically updates Cashbox.CurrentBalance.
    /// Direction.In adds to balance; Direction.Out subtracts from balance.
    /// </summary>
    Task RecordAsync(CashMovementRequest request, CancellationToken ct = default);
}
