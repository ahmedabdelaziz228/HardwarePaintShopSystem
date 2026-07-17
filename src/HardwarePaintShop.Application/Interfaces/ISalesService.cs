using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface ISalesService
{
    Task<List<SalesInvoiceListItem>> SearchAsync(
        string? query = null,
        CancellationToken cancellationToken = default);

    Task<SalesInvoiceDetails> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<List<SalesProductOption>> SearchProductsAsync(
        string query,
        Guid? priceGroupId = null,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveDraftAsync(
        SalesDraftRequest request,
        CancellationToken cancellationToken = default);

    Task PostAsync(
        PostSalesRequest request,
        CancellationToken cancellationToken = default);

    Task VoidDraftAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
