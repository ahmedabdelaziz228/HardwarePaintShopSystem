using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

/// <summary>
/// Records cash movements and atomically updates Cashbox.CurrentBalance.
/// Cashbox.CurrentBalance must NEVER be edited outside this service.
/// </summary>
public class CashMovementService : ICashMovementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CashMovementService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    /// <inheritdoc/>
    public async Task RecordAsync(CashMovementRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var cashbox = await db.Cashboxes.FindAsync(new object[] { request.CashboxId }, ct)
            ?? throw new InvalidOperationException($"Cashbox {request.CashboxId} not found.");

        var movement = new CashMovement
        {
            Id            = Guid.NewGuid(),
            CashboxId     = request.CashboxId,
            MovementType  = request.MovementType,
            Direction     = request.Direction,
            Amount        = request.Amount,
            ReferenceType = request.ReferenceType,
            ReferenceId   = request.ReferenceId,
            UserId        = request.UserId,
            Notes         = request.Notes,
            CreatedAt     = DateTime.UtcNow
        };

        await db.CashMovements.AddAsync(movement, ct);

        // Update balance atomically in the same transaction
        if (request.Direction == CashDirection.In)
            cashbox.CurrentBalance += request.Amount;
        else
            cashbox.CurrentBalance -= request.Amount;

        cashbox.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
