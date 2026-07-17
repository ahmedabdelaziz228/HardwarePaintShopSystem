using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Interfaces;

public interface IReturnService
{
    Task<List<ReturnSourceInvoice>> SearchSourceInvoicesAsync(ReturnType returnType, string? query = null, CancellationToken cancellationToken = default);
    Task<ReturnSourceDetails> GetSourceAsync(ReturnType returnType, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<List<ReturnListItem>> SearchReturnsAsync(string? query = null, CancellationToken cancellationToken = default);
    Task<Guid> PostAsync(PostReturnRequest request, CancellationToken cancellationToken = default);
}
