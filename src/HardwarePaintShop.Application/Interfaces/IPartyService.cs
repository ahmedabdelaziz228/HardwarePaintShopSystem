using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface IPartyService
{
    Task<List<CustomerListItem>> SearchCustomersAsync(
        PartySearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<CustomerDetails> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveCustomerAsync(
        CustomerSaveRequest request,
        CancellationToken cancellationToken = default);

    Task SetCustomerActiveAsync(
        Guid customerId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<CustomerStatementData> GetCustomerStatementAsync(
        Guid customerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<List<SupplierListItem>> SearchSuppliersAsync(
        PartySearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<SupplierDetails> GetSupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveSupplierAsync(
        SupplierSaveRequest request,
        CancellationToken cancellationToken = default);

    Task SetSupplierActiveAsync(
        Guid supplierId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
