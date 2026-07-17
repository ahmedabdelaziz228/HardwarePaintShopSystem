using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface ICashboxAdminService
{
    Task<List<CashboxListItem>> GetAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveAsync(
        CashboxSaveRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid cashboxId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
