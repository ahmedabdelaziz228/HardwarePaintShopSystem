using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductListItem>> SearchAsync(
        ProductSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<ProductDetails> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveAsync(
        ProductSaveRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid productId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<List<PriceInquiryResult>> InquirePriceAsync(
        string query,
        Guid? priceGroupId = null,
        CancellationToken cancellationToken = default);
}
