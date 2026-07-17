using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

/// <summary>
/// Simple audit log service. Creates immutable AuditLog records.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AuditService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    /// <inheritdoc/>
    public async Task LogAsync(
        string tableName,
        Guid recordId,
        AuditAction action,
        Guid? changedByUserId,
        string? oldValuesJson,
        string? newValuesJson,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entry = new AuditLog
        {
            Id              = Guid.NewGuid(),
            TableName       = tableName,
            RecordId        = recordId,
            Action          = action,
            ChangedByUserId = changedByUserId,
            OldValuesJson   = oldValuesJson,
            NewValuesJson   = newValuesJson,
            CreatedAt       = DateTime.UtcNow
        };

        await db.AuditLogs.AddAsync(entry, ct);
        await db.SaveChangesAsync(ct);
    }
}
