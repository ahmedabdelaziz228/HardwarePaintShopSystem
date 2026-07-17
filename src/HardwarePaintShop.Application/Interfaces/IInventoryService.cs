using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface IInventoryService
{
    Task<List<StockBalanceItem>> GetStockAsync(string? query = null, bool lowOnly = false, CancellationToken cancellationToken = default);
    Task<List<StockMovementListItem>> GetMovementsAsync(Guid? productId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<List<InventoryCountListItem>> GetCountsAsync(CancellationToken cancellationToken = default);
    Task<Guid> PostCountAsync(InventoryCountRequest request, CancellationToken cancellationToken = default);
    Task<List<AlertListItem>> RefreshAlertsAsync(bool unreadOnly = false, CancellationToken cancellationToken = default);
    Task MarkAlertReadAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task MarkAllAlertsReadAsync(CancellationToken cancellationToken = default);
}
