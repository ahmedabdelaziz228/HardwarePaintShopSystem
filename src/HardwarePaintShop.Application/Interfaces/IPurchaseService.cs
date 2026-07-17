using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface IPurchaseService
{
    Task<List<PurchaseInvoiceListItem>> SearchAsync(
        string? query = null,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceDetails> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<List<PurchaseProductOption>> SearchProductsAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveDraftAsync(
        PurchaseDraftRequest request,
        CancellationToken cancellationToken = default);

    Task PostAsync(
        PostPurchaseRequest request,
        CancellationToken cancellationToken = default);

    Task VoidAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
