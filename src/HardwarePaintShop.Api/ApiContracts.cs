using System.Text.Json;

namespace HardwarePaintShop.Api;

public sealed record LoginRequest(string Username, string Password, string DeviceName);
public sealed record LoginResponse(
    string Token, DateTime ExpiresAt, Guid UserId, string UserName,
    Guid DeviceId, IReadOnlyCollection<string> Permissions);

public sealed record ApiUserContext(
    Guid UserId,
    string UserName,
    Guid RoleId,
    Guid DeviceId,
    DateTime ExpiresAt,
    IReadOnlySet<string> Permissions)
{
    public const string ItemKey = "HardwarePaintShop.ApiUser";
    public bool Can(string permission) => Permissions.Contains(permission);

    public static ApiUserContext? From(HttpContext context)
        => context.Items.TryGetValue(ItemKey, out var value) ? value as ApiUserContext : null;
}

public sealed record ProductCreateRequest(
    string Name,
    string? ProductCode,
    Guid BaseUnitId,
    Guid? CategoryId,
    Guid? MainSupplierId,
    decimal MinStockBaseQuantity,
    bool IsSerialTracked,
    string? Barcode,
    Guid? PriceGroupId,
    decimal? SalePrice,
    decimal? MinSalePrice,
    string? ImageBase64,
    string? Notes);

public sealed record CustomerPaymentRequest(
    decimal Amount,
    Guid CashboxId,
    DateTime OperationDate,
    string? Notes);

public sealed record StockAdjustmentRequest(
    Guid ProductId,
    decimal QuantityBase,
    string Reason,
    DateTime OperationDate);

public sealed record SyncUploadRequest(IReadOnlyCollection<ClientOperation> Operations);
public sealed record ClientOperation(Guid OperationId, string Type, JsonElement Payload, DateTime CreatedAt);
public sealed record ClientOperationResult(Guid OperationId, string Status, string? Message, Guid? ServerRecordId);

internal sealed class StoredApiSession
{
    public Guid UserId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
