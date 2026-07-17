using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

/// <summary>
/// Records stock movements. Does NOT touch product.StockQuantity — there is none.
/// Stock is always derived by summing StockMovement.QuantityBaseUnit for a product.
/// </summary>
public class StockMovementService : IStockMovementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public StockMovementService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    /// <inheritdoc/>
    public async Task RecordAsync(StockMovementRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var movement = new StockMovement
        {
            Id               = Guid.NewGuid(),
            ProductId        = request.ProductId,
            ProductUnitId    = request.ProductUnitId,
            QuantityBaseUnit = request.QuantityBaseUnit,
            MovementType     = request.MovementType,
            ReferenceType    = request.ReferenceType,
            ReferenceId      = request.ReferenceId,
            UserId           = request.UserId,
            Notes            = request.Notes,
            CreatedAt        = DateTime.UtcNow
        };

        await db.StockMovements.AddAsync(movement, ct);
        await db.SaveChangesAsync(ct);
    }
}
