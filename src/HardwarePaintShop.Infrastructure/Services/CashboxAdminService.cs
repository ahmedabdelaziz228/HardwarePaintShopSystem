using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class CashboxAdminService : ICashboxAdminService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;

    public CashboxAdminService(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task<List<CashboxListItem>> GetAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Cashboxes.AsNoTracking();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query.OrderBy(c => c.Name)
            .Select(c => new CashboxListItem(
                c.Id, c.Name, c.OpeningBalance, c.CurrentBalance, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> SaveAsync(
        CashboxSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
            throw new InvalidOperationException("اسم الخزينة مطلوب.");
        if (request.OpeningBalance < 0)
            throw new InvalidOperationException("الرصيد الافتتاحي لا يمكن أن يكون سالبًا.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.Cashboxes.AnyAsync(
                c => c.Name == name && (!request.Id.HasValue || c.Id != request.Id.Value),
                cancellationToken))
        {
            throw new InvalidOperationException("يوجد خزينة بنفس الاسم.");
        }

        Cashbox cashbox;
        if (request.Id.HasValue)
        {
            cashbox = await db.Cashboxes.SingleOrDefaultAsync(c => c.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("الخزينة المطلوبة غير موجودة.");
            if (!request.IsActive && cashbox.CurrentBalance != 0)
                throw new InvalidOperationException("لا يمكن تعطيل خزينة رصيدها غير صفر.");
            cashbox.Name = name;
            cashbox.IsActive = request.IsActive;
            cashbox.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            cashbox = new Cashbox
            {
                Id = Guid.NewGuid(),
                Name = name,
                OpeningBalance = request.OpeningBalance,
                CurrentBalance = request.OpeningBalance,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await db.Cashboxes.AddAsync(cashbox, cancellationToken);

            if (request.OpeningBalance > 0)
            {
                await db.CashMovements.AddAsync(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    CashboxId = cashbox.Id,
                    MovementType = CashMovementType.OpeningBalance,
                    Direction = CashDirection.In,
                    Amount = request.OpeningBalance,
                    ReferenceType = "Cashbox",
                    ReferenceId = cashbox.Id,
                    UserId = _authService.CurrentUserId,
                    Notes = "رصيد افتتاحي للخزينة",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return cashbox.Id;
    }

    public async Task SetActiveAsync(
        Guid cashboxId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var cashbox = await db.Cashboxes.SingleOrDefaultAsync(c => c.Id == cashboxId, cancellationToken)
            ?? throw new KeyNotFoundException("الخزينة المطلوبة غير موجودة.");

        if (!isActive && cashbox.CurrentBalance != 0)
            throw new InvalidOperationException("لا يمكن تعطيل خزينة رصيدها غير صفر.");

        cashbox.IsActive = isActive;
        cashbox.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
