using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Interfaces;

/// <summary>
/// Service for recording immutable audit log entries for sensitive data changes.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Creates an audit log entry for a record change.
    /// </summary>
    /// <param name="tableName">Database table / entity name.</param>
    /// <param name="recordId">Primary key of the affected record.</param>
    /// <param name="action">Type of action performed.</param>
    /// <param name="changedByUserId">User who performed the action (null for system actions).</param>
    /// <param name="oldValuesJson">JSON of previous state (null for inserts).</param>
    /// <param name="newValuesJson">JSON of new state (null for deletes).</param>
    /// <param name="ct">Cancellation token.</param>
    Task LogAsync(
        string tableName,
        Guid recordId,
        AuditAction action,
        Guid? changedByUserId,
        string? oldValuesJson,
        string? newValuesJson,
        CancellationToken ct = default);
}
